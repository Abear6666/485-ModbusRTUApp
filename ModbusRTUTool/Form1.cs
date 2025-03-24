using Microsoft.Win32;
using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace ModbusRTUTool
{

    public partial class Form1 : Form
    {
        private SerialPort serialPort;
        private List<byte> _receiveBuffer = new List<byte>();  // 接收缓冲区
        private object _bufferLock = new object();             // 线程锁

        public Form1()
        {
            InitializeComponent();
            InitializeSerialPort();
        }
        private void InitializeSerialPort()
        {
            // 获取可用串口列表
            comboBoxPort.Items.AddRange(SerialPort.GetPortNames());
            comboBoxPort.SelectedIndex = comboBoxPort.Items.Count > 0 ? 0 : -1;

            // 波特率预设值
            comboBoxBaudRate.Items.AddRange(new object[] { 9600, 19200, 38400, 57600, 115200 });
            comboBoxBaudRate.SelectedIndex = 0;

            serialPort = new SerialPort();
            serialPort.DataReceived += SerialPort_DataReceived;
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (!serialPort.IsOpen)
                {
                    // 检查端口是否存在（防止用户手动输入无效端口）
                    string[] availablePorts = SerialPort.GetPortNames();
                    if (!availablePorts.Contains(comboBoxPort.Text))
                    {
                        MessageBox.Show("端口不存在！");
                        return;
                    }

                    // 设置参数
                    serialPort.PortName = comboBoxPort.Text;
                    serialPort.BaudRate = int.Parse(comboBoxBaudRate.Text);
                    serialPort.Parity = Parity.None;
                    serialPort.DataBits = 8;
                    serialPort.StopBits = StopBits.One;

                    // 打开端口
                    serialPort.Open();
                    buttonConnect.Text = "关闭端口";
                    AppendLog("串口已打开");
                }
                else
                {
                    serialPort.Close();
                    buttonConnect.Text = "打开端口";
                    AppendLog("串口已关闭");
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("端口被占用或无权限访问！");
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"参数错误: {ex.Message}");
            }
            catch (FormatException)
            {
                MessageBox.Show("波特率必须为整数！");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}");
            }
        }


        // CRC16校验计算（Modbus标准）
        private ushort CalculateCRC(byte[] data)
        {
            ushort crc = 0xFFFF;
            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    bool lsb = (crc & 0x0001) != 0;
                    crc >>= 1;
                    if (lsb) crc ^= 0xA001;
                }
            }
            return crc;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            RefreshPortList();
        }
        // 刷新端口列表方法
        private void RefreshPortList()
        {
            comboBoxPort.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            comboBoxPort.Items.AddRange(ports);
            comboBoxPort.SelectedIndex = ports.Length > 0 ? 0 : -1;
        }
        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (!serialPort.IsOpen)
            {
                MessageBox.Show("请先打开串口！");
                return;
            }

            string input = textBoxSend.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("请输入指令！");
                return;
            }

            byte[] rawCommand = HexStringToBytes(input);
            if (rawCommand == null)
            {
                MessageBox.Show("输入格式错误，请使用十六进制（如 01 03 00 00 00 02）");
                return;
            }

            byte[] fullCommand = AddCRCToCommand(rawCommand);
            try
            {
                serialPort.Write(fullCommand, 0, fullCommand.Length);
                AppendLog($"发送: {BitConverter.ToString(fullCommand)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送失败: {ex.Message}");
            }
        }
        // 将十六进制字符串转换为字节数组（支持空格分隔）
        private byte[] HexStringToBytes(string input)
        {
            try
            {
                // 移除空格和其他非Hex字符
                input = input.Replace(" ", "").Replace("0x", "").Trim();
                if (input.Length % 2 != 0)
                    throw new ArgumentException("输入长度必须为偶数");

                byte[] bytes = new byte[input.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    string hex = input.Substring(i * 2, 2);
                    bytes[i] = Convert.ToByte(hex, 16);
                }
                return bytes;
            }
            catch
            {
                return null;
            }
        }

        // 为原始指令添加CRC校验
        private byte[] AddCRCToCommand(byte[] rawCommand)
        {
            if (rawCommand == null || rawCommand.Length == 0)
                return null;

            ushort crc = CalculateCRC(rawCommand);
            byte[] fullCommand = new byte[rawCommand.Length + 2];
            Array.Copy(rawCommand, 0, fullCommand, 0, rawCommand.Length);
            fullCommand[fullCommand.Length - 2] = (byte)(crc & 0xFF);  // CRC低字节在前
            fullCommand[fullCommand.Length - 1] = (byte)(crc >> 8);
            return fullCommand;
        }

        // 接收数据处理
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (!serialPort.IsOpen) return;

                byte[] buffer = new byte[serialPort.BytesToRead];
                serialPort.Read(buffer, 0, buffer.Length);

                lock (_bufferLock)
                {
                    _receiveBuffer.AddRange(buffer);  // 数据累积
                }

                ProcessBuffer();  // 处理缓冲区
            }
            catch (Exception ex)
            {
                AppendLog($"接收异常: {ex.Message}");
            }
        }
        private void ProcessBuffer()
        {
            lock (_bufferLock)
            {
                while (_receiveBuffer.Count >= 5)  // 最小帧长度校验
                {
                    // 动态计算完整帧长度
                    int expectedLength = GetExpectedFrameLength(_receiveBuffer);
                    if (expectedLength == 0 || _receiveBuffer.Count < expectedLength)
                        return;

                    // 提取完整帧
                    byte[] frame = _receiveBuffer.GetRange(0, expectedLength).ToArray();
                    _receiveBuffer.RemoveRange(0, expectedLength);

                    // 校验CRC后输出原始数据
                    if (CheckCRC(frame))
                    {
                        AppendLog($"响应: {BitConverter.ToString(frame)}");
                    }
                    else
                    {
                        AppendLog($"无效帧(CRC错误): {BitConverter.ToString(frame)}");
                    }
                }
            }
        }

        // 根据功能码判断期望帧长度
        private int GetExpectedFrameLength(List<byte> buffer)
        {
            if (buffer.Count < 2) return 0;
            byte functionCode = buffer[1];

            switch (functionCode)
            {
                case 0x03:  // 读保持寄存器
                    if (buffer.Count < 3) return 0;
                    int byteCount = buffer[2];
                    return 1 + 1 + 1 + byteCount + 2;  // 地址+功能码+字节数+数据+CRC
                case 0x06:  // 写单个寄存器
                    return 6 + 2;  // 地址+功能码+地址+值+CRC（固定8字节）
                case 0x10:  // 写多个寄存器
                    return 6 + 2;  // 地址+功能码+地址+寄存器数目+CRC（固定8字节）
                default:
                    return 0;  // 其他功能码暂不处理
            }
        }

        // CRC校验
        private bool CheckCRC(byte[] frame)
        {
            if (frame.Length < 2) return false;
            ushort receivedCRC = (ushort)(frame[frame.Length - 1] << 8 | frame[frame.Length - 2]);
            byte[] dataWithoutCRC = new byte[frame.Length - 2];
            Array.Copy(frame, 0, dataWithoutCRC, 0, dataWithoutCRC.Length);
            ushort calculatedCRC = CalculateCRC(dataWithoutCRC);
            return receivedCRC == calculatedCRC;
        }


        private void AppendLog(string message)
        {
            if (richTextBoxRecv.IsDisposed) return;

            if (richTextBoxRecv.InvokeRequired)
            {
                richTextBoxRecv.BeginInvoke(new Action(() =>
                {
                    richTextBoxRecv.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
                }));
            }
            else
            {
                richTextBoxRecv.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
            }
        }

        private void textBoxSend_TextChanged(object sender, EventArgs e)
        {
            string input = textBoxSend.Text.Replace(" ", "");
            bool isValid = input.Length % 2 == 0 && System.Text.RegularExpressions.Regex.IsMatch(input, @"^[0-9a-fA-F]+$");
            textBoxSend.BackColor = isValid ? Color.White : Color.LightPink;
        }
    }
}
