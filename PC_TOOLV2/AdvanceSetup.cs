// Import required libraries for handling UI, image processing, serial communication, etc.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PC_TOOLV2
{
    public partial class AdvanceSetup : Form
    {
        // Declare an instance of SerialPortInfor_t to store the serial port information
        SerialPortInfor_t SerialPort_infor = new SerialPortInfor_t();

        /*
            Event declaration to update the serial port information
            This event will be triggered when the serial port settings need to be updated
         */
        public event EventHandler<SerialPortInfor_t> UpdateSerialPort;

        // Method to update the serial port settings received from another form or process
        public void ReceivedSerialPort(SerialPortInfor_t serialPort)
        {
            // Update the SerialPort_infor object with the received serial port details
            SerialPort_infor.PortName = serialPort.PortName;
            SerialPort_infor.Baudrate = serialPort.Baudrate;
        }

        // Stopwatch for performance tracking or measuring elapsed time, but it's not used in this code
        private Stopwatch stopwatch;

        // Constructor for the AdvanceSetup form
        public AdvanceSetup()
        {
            InitializeComponent();  // Initializes the form components
            stopwatch = new Stopwatch();  // Initializes the stopwatch object
            stopwatch.Stop();  // Stops the stopwatch (not used here)
        }

        // Event handler when the form loads
        private void Form2_Load(object sender, EventArgs e)
        {
            // Array of supported baud rates for the serial port
            string[] baudrate = { "9600", "57600", "115200" };

            // Get the available COM port names on the machine
            string[] porrName = SerialPort.GetPortNames();

            // Populate the ComboBox for port names and baud rates
            listPortCb.DataSource = porrName;
            listBaundrate.DataSource = baudrate;

            // Set the selected item to the existing serial port settings (if any)
            listPortCb.SelectedItem = SerialPort_infor.PortName;
            listBaundrate.SelectedItem = SerialPort_infor.Baudrate.ToString();
        }

        // Event handler when the 'Connect' button is clicked
        private void connectBtn_Click(object sender, EventArgs e)
        {
            // Convert the baud rate from text to integer and assign to SerialPort_infor
            Int32.TryParse(listBaundrate.Text.ToString(), out SerialPort_infor.Baudrate);

            // Get the selected port name and assign to SerialPort_infor
            SerialPort_infor.PortName = listPortCb.SelectedValue.ToString();

            // Trigger the UpdateSerialPort event to notify other components of the updated settings
            UpdateSerialPort?.Invoke(this, SerialPort_infor);

            // Close the form after updating the serial port settings
            this.Close();
        }

        // Event handler for when the first button is clicked to refresh the available COM ports and baud rates
        private void button1_Click(object sender, EventArgs e)
        {
            // Define available baud rates
            string[] baudrate = { "9600", "57600", "115200" };

            // Get available COM port names
            string[] porrName = SerialPort.GetPortNames();

            // Refresh the ComboBox for COM port names and baud rates
            listPortCb.DataSource = porrName;
            listBaundrate.DataSource = baudrate;
        }
    }
}
