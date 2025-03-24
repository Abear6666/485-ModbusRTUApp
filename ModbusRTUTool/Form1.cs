using Microsoft.Win32;
using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace ModbusRTUTool
{

    public partial class Form1 : Form
    {
        private SerialPort serialPort;
        private List<byte> _receiveBuffer = new List<byte>();  // ���ջ�����
        private object _bufferLock = new object();             // �߳���

        public Form1()
        {
            InitializeComponent();
            InitializeSerialPort();
        }
        private void InitializeSerialPort()
        {
            // ��ȡ���ô����б�
            comboBoxPort.Items.AddRange(SerialPort.GetPortNames());
            comboBoxPort.SelectedIndex = comboBoxPort.Items.Count > 0 ? 0 : -1;

            // ������Ԥ��ֵ
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
                    // ���˿��Ƿ���ڣ���ֹ�û��ֶ�������Ч�˿ڣ�
                    string[] availablePorts = SerialPort.GetPortNames();
                    if (!availablePorts.Contains(comboBoxPort.Text))
                    {
                        MessageBox.Show("�˿ڲ����ڣ�");
                        return;
                    }

                    // ���ò���
                    serialPort.PortName = comboBoxPort.Text;
                    serialPort.BaudRate = int.Parse(comboBoxBaudRate.Text);
                    serialPort.Parity = Parity.None;
                    serialPort.DataBits = 8;
                    serialPort.StopBits = StopBits.One;

                    // �򿪶˿�
                    serialPort.Open();
                    buttonConnect.Text = "�رն˿�";
                    AppendLog("�����Ѵ�");
                }
                else
                {
                    serialPort.Close();
                    buttonConnect.Text = "�򿪶˿�";
                    AppendLog("�����ѹر�");
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("�˿ڱ�ռ�û���Ȩ�޷��ʣ�");
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"��������: {ex.Message}");
            }
            catch (FormatException)
            {
                MessageBox.Show("�����ʱ���Ϊ������");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"����ʧ��: {ex.Message}");
            }
        }


        // CRC16У����㣨Modbus��׼��
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
        // ˢ�¶˿��б���
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
                MessageBox.Show("���ȴ򿪴��ڣ�");
                return;
            }

            string input = textBoxSend.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("������ָ�");
                return;
            }

            byte[] rawCommand = HexStringToBytes(input);
            if (rawCommand == null)
            {
                MessageBox.Show("�����ʽ������ʹ��ʮ�����ƣ��� 01 03 00 00 00 02��");
                return;
            }

            byte[] fullCommand = AddCRCToCommand(rawCommand);
            try
            {
                serialPort.Write(fullCommand, 0, fullCommand.Length);
                AppendLog($"����: {BitConverter.ToString(fullCommand)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"����ʧ��: {ex.Message}");
            }
        }
        // ��ʮ�������ַ���ת��Ϊ�ֽ����飨֧�ֿո�ָ���
        private byte[] HexStringToBytes(string input)
        {
            try
            {
                // �Ƴ��ո��������Hex�ַ�
                input = input.Replace(" ", "").Replace("0x", "").Trim();
                if (input.Length % 2 != 0)
                    throw new ArgumentException("���볤�ȱ���Ϊż��");

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

        // Ϊԭʼָ�����CRCУ��
        private byte[] AddCRCToCommand(byte[] rawCommand)
        {
            if (rawCommand == null || rawCommand.Length == 0)
                return null;

            ushort crc = CalculateCRC(rawCommand);
            byte[] fullCommand = new byte[rawCommand.Length + 2];
            Array.Copy(rawCommand, 0, fullCommand, 0, rawCommand.Length);
            fullCommand[fullCommand.Length - 2] = (byte)(crc & 0xFF);  // CRC���ֽ���ǰ
            fullCommand[fullCommand.Length - 1] = (byte)(crc >> 8);
            return fullCommand;
        }

        // �������ݴ���
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (!serialPort.IsOpen) return;

                byte[] buffer = new byte[serialPort.BytesToRead];
                serialPort.Read(buffer, 0, buffer.Length);

                lock (_bufferLock)
                {
                    _receiveBuffer.AddRange(buffer);  // �����ۻ�
                }

                ProcessBuffer();  // ��������
            }
            catch (Exception ex)
            {
                AppendLog($"�����쳣: {ex.Message}");
            }
        }
        private void ProcessBuffer()
        {
            lock (_bufferLock)
            {
                while (_receiveBuffer.Count >= 5)  // ��С֡����У��
                {
                    // ��̬��������֡����
                    int expectedLength = GetExpectedFrameLength(_receiveBuffer);
                    if (expectedLength == 0 || _receiveBuffer.Count < expectedLength)
                        return;

                    // ��ȡ����֡
                    byte[] frame = _receiveBuffer.GetRange(0, expectedLength).ToArray();
                    _receiveBuffer.RemoveRange(0, expectedLength);

                    // У��CRC�����ԭʼ����
                    if (CheckCRC(frame))
                    {
                        AppendLog($"��Ӧ: {BitConverter.ToString(frame)}");
                    }
                    else
                    {
                        AppendLog($"��Ч֡(CRC����): {BitConverter.ToString(frame)}");
                    }
                }
            }
        }

        // ���ݹ������ж�����֡����
        private int GetExpectedFrameLength(List<byte> buffer)
        {
            if (buffer.Count < 2) return 0;
            byte functionCode = buffer[1];

            switch (functionCode)
            {
                case 0x03:  // �����ּĴ���
                    if (buffer.Count < 3) return 0;
                    int byteCount = buffer[2];
                    return 1 + 1 + 1 + byteCount + 2;  // ��ַ+������+�ֽ���+����+CRC
                case 0x06:  // д�����Ĵ���
                    return 6 + 2;  // ��ַ+������+��ַ+ֵ+CRC���̶�8�ֽڣ�
                case 0x10:  // д����Ĵ���
                    return 6 + 2;  // ��ַ+������+��ַ+�Ĵ�����Ŀ+CRC���̶�8�ֽڣ�
                default:
                    return 0;  // �����������ݲ�����
            }
        }

        // CRCУ��
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
