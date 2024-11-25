// Import required libraries for handling UI, image processing, serial communication, etc.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;

// Define the main namespace for the PC Tool application
namespace PC_TOOLV2
{
    // Main class to manage the interface and logic of the PC Tool
    public partial class MainForm : Form
    {
        /*
         * Main thread for executing the core functionalities of the PC Tool.
         * This ensures background processing without freezing the UI.
         */
        private Thread MainFunctionThread = null;

        /*
         * Stores the original image of the steering wheel.
         * This image is used for rotation based on the received angle.
         */
        private Image originalImag = null;

        /*
         * A structure containing information about the rotation angle and distance
         * (defined in the `Information_t` class). This information is updated from the received data.
         */
        private Information_t Information = new Information_t();

        /*
         * Warning thresholds for rotation angle and distance.
         * Default values are 30° for the angle and 50 cm for the distance, configurable via a file or UI.
         */
        private Information_t Data_Warning = new Information_t(30, 50);

        /*
         * Current and previous configurations of the Serial Port.
         * Used to handle changes in the connection settings.
         */
        private SerialPortInfor_t serialPort_Current = new SerialPortInfor_t();
        private SerialPortInfor_t serialPort_Old = new SerialPortInfor_t();

        /*
         * The current state of the PC Tool, defined by the `PCToolState_t` enum.
         * This state helps manage the tool's logic during different stages,
         * such as initialization, running, pausing, or connection loss.
         */
        private PCToolState_t pcToolState = PCToolState_t.PCTool_Deinit;

        /*
         * A queue for storing data received from the Serial Port.
         * This data will be processed in the main thread.
         */
        private Queue<Message_t> dataReceivedQueue = new Queue<Message_t>();

        /*
         * A queue for managing requests and responses.
         * This queue handles two-way communication between the PC Tool and the device.
         */
        private Queue<Message_t> requestResponseQueue = new Queue<Message_t>();

        /*
         * A dictionary mapping data IDs to their corresponding data strings.
         * This allows quick lookup of information based on the ID.
         */
        private Dictionary<int, string> mappingData = new Dictionary<int, string>();

        /*
         * A queue for storing configuration changes to the Serial Port.
         * New configurations are processed in the main thread.
         */
        private Queue<SerialPortInfor_t> serialPortInforQueue = new Queue<SerialPortInfor_t>();

        /*
         * An object for handling and parsing data received from the device.
         * `Message_t` contains information about the ID and raw data.
         */
        private Message_t ParseData = new Message_t();

        /*
         * IDs used for receiving rotation angle and distance data.
         * Default values are "192" for the angle and "208" for the distance.
         */
        private string IdDataNode1 = "192";
        private string IdDataNode2 = "208";
        private string IdRequestConnectNode1 = "161";
        private string IdRequestConnectNode2 = "162";
        private string IdRequestConnectForwader = "160";
        private string DataConfirmConnect = "255";
        private string DataConfirmData = "65535";
        private string DataRequestConnect ="10";
        /*
         * A filter for specific response IDs.
         * Only messages with matching IDs are processed.
         */
        private string IdFilterResponse = "";

        /*
         * Current mapping ID value.
         * Used to identify data when adding new entries.
         */
        private int IdMappingData;

        /*
         * Stopwatch objects for measuring execution time of tasks.
         * These are used for timeout management and performance checks.
         */
        private Stopwatch TimerCounter = new Stopwatch();
        private Stopwatch checkTimeout = new Stopwatch();
        private Stopwatch pingTimeout = new Stopwatch();

        /*
         * Path to the configuration Data Warning file.
         * This file stores warning thresholds for rotation angle and distance.
         */
        private string configDataWarningPath = "./configDataFile.txt";

        /*
         * Path to the configuration Data Warning file.
         * This file stores configuration for serialport.
         */
        private string configSerialPath = "./configSerialFile.txt";

        /*
         * Method to check if a response ID exists in the `mappingData` dictionary.
         */
        private bool isResponExist(int ID)
        {
            return mappingData.ContainsKey(ID);
        }

        /*
         * Method to generate a random ID within the range 1 to 999.
         * This ID is used for identifying data or requests.
         */
        private int generateID()
        {
            return new Random().Next(1, 999);
        }

        /*
         * Main constructor for the `MainForm` class.
         * Calls `InitializeComponent` to set up the UI components.
         */
        public MainForm()
        {
            InitializeComponent();
        }

        /*
         * Event triggered when the form is loaded.
         * Used to initialize parameters, load the image, and read the configuration file.
         */
        private void Form1_Load(object sender, EventArgs e)
        {
            string buffer = null;

            // Load the original image for the steering wheel
            originalImag = volang_picturebox.Image;

            // Read warning thresholds from the warning configuration file, if it exists
            if (File.Exists(configDataWarningPath))
            {
                buffer = File.ReadAllText(configDataWarningPath);
                int.TryParse(buffer.Split(':')[0], out Data_Warning.Rotaion);
                int.TryParse(buffer.Split(':')[1], out Data_Warning.Distance);
            }
            // Read infor of serial port from the serial port configuration file, if it exists
            if (File.Exists(configSerialPath))
            {
                buffer = File.ReadAllText(configSerialPath);
                serialPort_Current.PortName = buffer.Split(':')[0];
                int.TryParse(buffer.Split(':')[1], out serialPort_Current.Baudrate);
            }
            else
            {
                serialPort_Current.Baudrate = 9600;
                serialPort_Current.PortName = "COM9";
            }

            // Update labels to display the safety thresholds
            rotationWarningLabel.Text = "Safety: " + Data_Warning.Rotaion.ToString() + "°";
            distanceWarningLabel.Text = "Safety: " + Data_Warning.Distance.ToString() + "cm";

            // Configure default Serial Port settings
            Forwader.PortName = serialPort_Current.PortName;
            Forwader.BaudRate = serialPort_Current.Baudrate;

            // Start the main processing thread
            StartThread();
        }

        /*
         * Method to start the main thread.
         * This thread continuously executes tasks like managing states and processing data.
         */
        private void StartThread()
        {
            MainFunctionThread = new Thread(new ThreadStart(PCToolMainFunction));
            MainFunctionThread.IsBackground = true;
            MainFunctionThread.Start();
        }

        /*
         * Overrides the form's closing event to stop the main thread before exiting.
         * This ensures no threads are left running after the application ends.
         */
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (MainFunctionThread != null && MainFunctionThread.IsAlive)
            {
                MainFunctionThread.Abort();
            }
        }
        // Main function of the PC Tool, responsible for handling the main loop based on the current state.
        private void PCToolMainFunction()
        {
            while (true) // Main loop for continuously processing states.
            {
                switch (pcToolState) // Check the current state of the PC Tool.
                {
                    case PCToolState_t.PCTool_Deinit:
                        Task_PCToolDeinit(); // Perform actions when the PC Tool is in the Deinit state.
                        break;
                    case PCToolState_t.PCTool_Init:
                        Task_PCToolInit(); // Perform initialization actions when in the Init state.
                        break;
                    case PCToolState_t.PCTool_Running:
                        Task_PCToolRunning(); // Perform actions for the Running state.
                        break;
                    case PCToolState_t.PCTool_Pause:
                        Task_PCToolPause(); // Perform actions when the PC Tool is in the Pause state.
                        break;
                    case PCToolState_t.PCTool_Reconnect:
                        Task_PCToolReconnect(); // Handle reconnection attempts in the Reconnect state.
                        break;
                    case PCToolState_t.PCTool_Disconnected:
                        Task_PCToolDisconnected(); // Handle operations when in the Disconnected state.
                        break;
                    default:
                        break; // Handle any undefined states.
                }
                Thread.Sleep(1); // Add a small delay to avoid excessive CPU usage.
            }
        }

        // Function to transition from Deinit state to Init state.
        private void Task_PCToolDeinit()
        {
            pcToolState = PCToolState_t.PCTool_Init; // Set the state to Init for the next step.
        }

        // Function to initialize the PC Tool and establish initial connections.
        private void Task_PCToolInit()
        {
            SendRequestToNode(IdRequestConnectForwader, DataRequestConnect); // Send the first request to the forwader.
            if (checkRequestConnection(IdRequestConnectForwader, DataConfirmConnect) == true) // Check if the connection was successful.
            {
                SendRequestToNode(IdRequestConnectNode1, DataRequestConnect); // Send the second request to the node 1 .
                if (checkRequestConnection(IdRequestConnectNode1, DataConfirmConnect) == true)
                {
                    this.Invoke(new Action(() => // Update the UI to indicate connection success.
                    {
                        rotationLabel.Text = "Connected";
                    }));
                }
                else
                {
                    this.Invoke(new Action(() => // Update the UI to indicate disconnection.
                    {
                        rotationLabel.Text = "Disconnection";
                    }));
                }
                SendRequestToNode(IdRequestConnectNode2, DataRequestConnect); // Send the third request to the node 2.
                if (checkRequestConnection(IdRequestConnectNode2, DataConfirmConnect) == true)
                {
                    this.Invoke(new Action(() => // Update the UI to indicate connection success.
                    {
                        distanceLabel.Text = "Connected";
                    }));
                }
                else
                {
                    this.Invoke(new Action(() => // Update the UI to indicate disconnection.
                    {
                        distanceLabel.Text = "Disconnection";
                    }));
                }
                pcToolState = PCToolState_t.PCTool_Running; // Transition to the Running state.
            }
            else
            {
                pcToolState = PCToolState_t.PCTool_Disconnected; // Transition to the Disconnected state if initialization fails.
            }
        }

        // Function to handle the Disconnected state by transitioning to Reconnect.
        private void Task_PCToolDisconnected()
        {
            pcToolState = PCToolState_t.PCTool_Reconnect;
        }

        // Function to attempt reconnection when in the Reconnect state.
        private void Task_PCToolReconnect()
        {
            TimerCounter.Start(); // Start the reconnection timer.
            this.Invoke(new Action(() =>
            {
                statusConnectBtn.BackColor = Color.Yellow; // Update the UI to indicate reconnection attempt.
            }));
            while (TimerCounter.ElapsedMilliseconds < 1000) ; // Wait for the timer to reach 1 second.
            SendRequestToNode(IdRequestConnectForwader, DataRequestConnect); // Send a reconnection request.
            if (checkRequestConnection(IdRequestConnectForwader, DataConfirmConnect) == true) // Check if reconnection was successful.
            {
                pcToolState = PCToolState_t.PCTool_Running; // Transition to the Running state.
            }
            if (serialPortInforQueue.Count > 0) // If there are pending serial port changes, transition to Pause state.
            {
                pcToolState = PCToolState_t.PCTool_Pause;
            }
            TimerCounter.Stop();
            TimerCounter.Reset();
        }

        // Function to handle operations when the PC Tool is in the Running state.
        private void Task_PCToolRunning()
        {
            Message_t tpmMessage = new Message_t(); // Temporary message structure.
            this.Invoke(new Action(() =>
            {
                statusConnectBtn.BackColor = Color.Green; // Update UI to indicate a stable connection.
            }));
            if (dataReceivedQueue.Count > 0) // Check if there is incoming data to process.
            {
                tpmMessage = dataReceivedQueue.Dequeue(); // Retrieve the next message from the queue.
                SendResponseToForwader(tpmMessage.Id, DataConfirmData); // Send acknowledgment to the forwarder.

                if (String.Compare(tpmMessage.Id, IdDataNode1) == 0) // Handle messages from Node 1.
                {
                    if (String.Compare(tpmMessage.Message, DataConfirmData) == 0) // Check for disconnection.
                    {
                        this.Invoke(new Action(() =>
                        {
                            rotationLabel.Text = "Disconnected";
                            RotationWarningBtn.BackColor = Color.Yellow;
                        }));
                    }
                    else
                    {
                        // Process rotation data and update UI.
                        int.TryParse(tpmMessage.Message, out Information.Rotaion);
                        // Convert rotaion from 0-180 to (-90->90)
                        Information.Rotaion = Information.Rotaion - 90;
                        this.Invoke(new Action(() =>
                        {
                            rotationLabel.Text = Information.Rotaion.ToString() + "°";
                            if (Math.Abs(Information.Rotaion) > Data_Warning.Rotaion)
                            {
                                RotationWarningBtn.BackColor = Color.Red;
                            }
                            else
                            {
                                RotationWarningBtn.BackColor = Color.Green;
                            }
                        }));
                        volang_picturebox.Image = RotateImage(originalImag, Information.Rotaion);
                    }
                }
                else if (String.Compare(tpmMessage.Id, IdDataNode2) == 0) // Handle messages from Node 2.
                {
                    if (String.Compare(tpmMessage.Message, DataConfirmData) == 0) // Check for disconnection.
                    {
                        this.Invoke(new Action(() =>
                        {
                            distanceLabel.Text = "Disconnected";
                            DistanceWarningBtn.BackColor = Color.Yellow;
                        }));
                    }
                    else
                    {
                        // Process distance data and update UI.
                        int.TryParse(tpmMessage.Message, out Information.Distance);
                        this.Invoke(new Action(() =>
                        {
                            distanceLabel.Text = Information.Distance.ToString() + "cm";
                            if (Information.Distance < Data_Warning.Distance)
                            {
                                DistanceWarningBtn.BackColor = Color.Red;
                            }
                            else
                            {
                                DistanceWarningBtn.BackColor = Color.Green;
                            }
                        }));
                    }
                }
            }

            // Periodic check for timeout or disconnection.
            if (checkTimeout.ElapsedMilliseconds > 1000)
            {
                checkTimeout.Stop();
                checkTimeout.Reset();
                SendRequestToNode(IdRequestConnectForwader, DataRequestConnect); // Send periodic requests to maintain connection.
                if (checkRequestConnection(IdRequestConnectForwader, DataConfirmConnect) != true)
                {
                    pcToolState = PCToolState_t.PCTool_Disconnected; // Transition to Disconnected if no response.
                }
                else
                {
                    pcToolState = PCToolState_t.PCTool_Running; // Continue Running state.
                }
            }
            if (serialPortInforQueue.Count > 0) // If serial port updates are pending, transition to Pause state.
            {
                pcToolState = PCToolState_t.PCTool_Pause;
            }
        }

        // Function to handle the Pause state by updating the serial port configuration.
        private void Task_PCToolPause()
        {
            SerialPortInfor_t tpmSerialInfor = new SerialPortInfor_t(); // Temporary serial port info structure.
            if (Forwader.IsOpen == true && serialPortInforQueue.Count > 0)
            {
                tpmSerialInfor = serialPortInforQueue.Dequeue(); // Get the next serial port configuration.
                Forwader.Close();
                Forwader.PortName = tpmSerialInfor.PortName;
                Forwader.BaudRate = tpmSerialInfor.Baudrate;
            }
            else
            {
                tpmSerialInfor = serialPortInforQueue.Dequeue();
                Forwader.PortName = tpmSerialInfor.PortName;
                Forwader.BaudRate = tpmSerialInfor.Baudrate;
            }
            pcToolState = PCToolState_t.PCTool_Reconnect; // Transition to Reconnect after updating the serial port.
        }

        //Function to checks whether a valid response has been received from a request within a specified timeout period (100 ms).
        private bool checkRequestConnection(string ID, string Data)
        {
            bool retVal = true; // Default return value is true, assuming the connection is successful
            Message_t buffer = new Message_t(); // Temporary variable to hold the received message

            // Wait for a response or timeout (100ms)
            while (requestResponseQueue.Count == 0 && pingTimeout.ElapsedMilliseconds < 100) ;

            if (requestResponseQueue.Count > 0 && pingTimeout.ElapsedMilliseconds < 100)
            {
                // If a message is in the queue and within the timeout period
                buffer = requestResponseQueue.Dequeue(); // Dequeue the message from the queue

                // Compare the ID and message data with the expected values
                if (String.Compare(buffer.Id, ID) == 0 && String.Compare(buffer.Message, Data) == 0)
                {
                    retVal = true; // If both ID and Data match, set return value to true
                }
            }
            else
            {
                pingTimeout.Stop(); // Stop the timeout timer
                retVal = false; // If timeout occurs, return false
            }

            // Update the UI with the elapsed time of the ping timeout
            this.Invoke(new Action(() =>
            {
                pingLabel.Text = pingTimeout.ElapsedMilliseconds.ToString() + " ms"; // Display the elapsed time in milliseconds
            }));

            pingTimeout.Reset(); // Reset the timeout timer for the next request
            IdFilterResponse = ""; // Clear the response filter
            mappingData.Remove(IdMappingData); // Remove previous mapping data

            return retVal; // Return whether the connection was successful or not
        }

        //This Function used to sends a response to a forwarder via a serial connection
        private void SendResponseToForwader(string IdNode, string Data)
        {
            try
            {
                string message = IdNode + "-" + Data; // Create the message string by concatenating the node ID and data

                if (Forwader.IsOpen == true) // Check if the forwarder serial port is open
                {
                    Forwader.WriteLine(message); // Send the message to the forwarder
                }
            }
            catch (Exception e)
            {
                // Handle any exception (no specific handling in this code)
            }
        }

        // This function used to send request to the a node via the forwarder serial port, ensuring the port is open before sending.
        private void SendRequestToNode(string IdNode, string Data)
        {
            string message = IdNode + "-" + Data; // Create the message string
            IdMappingData = generateID(); // Generate a unique ID for the request
            IdFilterResponse = IdNode; // Set the node ID to filter responses

            // Ensure the forwarder port is open before sending the message
            if (Forwader.IsOpen != true)
            {
                try
                {
                    Forwader.Open(); // Attempt to open the port if it is closed
                }
                catch (Exception ex)
                {
                    // Handle any exceptions (no specific handling in this code)
                }
            }

            if (Forwader.IsOpen == true) // Check if the port is successfully opened
            {
                Forwader.WriteLine(message); // Send the request message
            }

            pingTimeout.Restart(); // Restart the timeout timer for waiting for the response
        }

        //This function parses a message string into an ID and message component.
        private void parseMessage(string message)
        {
            string[] buffer = message.Split('-'); // Split the message into parts using the hyphen as a delimiter
            ParseData.Id = buffer[0]; // First part is the ID
            ParseData.Message = buffer[1]; // Second part is the message
        }

        //This function rotates an image by a given angle around its center.
        private Image RotateImage(Image img, float angle)
        {
            Bitmap rotateBmp = new Bitmap(img.Width, img.Height); // Create a new bitmap for the rotated image
            using (Graphics g = Graphics.FromImage(rotateBmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic; // Set high-quality interpolation mode
                g.SmoothingMode = SmoothingMode.AntiAlias; // Apply anti-aliasing for smooth edges
                g.PixelOffsetMode = PixelOffsetMode.HighQuality; // Use high-quality pixel offset mode

                // Translate the origin to the center of the image
                g.TranslateTransform((float)img.Width / 2, (float)img.Height / 2);

                // Rotate the image by the specified angle
                g.RotateTransform(angle);

                // Draw the rotated image onto the bitmap
                g.DrawImage(img, -img.Width / 2, -img.Height / 2);
            }
            return rotateBmp; // Return the rotated image
        }

        // This function updates the UI with the latest safety information, including rotation and distance
        private void UpdateInforWarning(object sender, Information_t setup)
        {
            Data_Warning.Rotaion = setup.Rotaion; // Update rotation value
            Data_Warning.Distance = setup.Distance; // Update distance value

            // Update the UI labels with the new rotation and distance values
            rotationWarningLabel.Text = "Safety: " + Data_Warning.Rotaion.ToString() + "°";
            distanceWarningLabel.Text = "Safety: " + Data_Warning.Distance.ToString() + "cm";

            // Save the warning data to a file
            File.WriteAllText(configDataWarningPath, Data_Warning.Rotaion.ToString() + ":" + Data_Warning.Distance.ToString());
        }

        //This function updates the serial port information and handles changes.
        private void UpdateSerialPort(object sender, SerialPortInfor_t SerialPortInfor)
        {
            // Store the current serial port information for comparison
            serialPort_Old.PortName = serialPort_Current.PortName;
            serialPort_Old.Baudrate = serialPort_Current.Baudrate;

            // Update the current serial port information with the new data
            serialPort_Current.PortName = SerialPortInfor.PortName;
            serialPort_Current.Baudrate = SerialPortInfor.Baudrate;

            // If there is a change in port or baud rate, pause the tool and enqueue the new port information
            if (String.Compare(serialPort_Old.PortName, serialPort_Current.PortName) != 0 ||
                serialPort_Old.Baudrate != serialPort_Current.Baudrate)
            {
                pcToolState = PCToolState_t.PCTool_Pause;
                serialPortInforQueue.Enqueue(new SerialPortInfor_t(serialPort_Current.PortName, serialPort_Current.Baudrate));
                File.WriteAllText(configSerialPath, serialPort_Current.PortName + ":" + serialPort_Current.Baudrate.ToString());
            }
        }

        //This function handles the click event for a button that opens a new form (AdvanceSetup)
        private void button2_Click(object sender, EventArgs e)
        {
            AdvanceSetup advanceModeForm = new AdvanceSetup(); // Create a new form for advanced setup
            advanceModeForm.UpdateSerialPort += UpdateSerialPort; // Subscribe to the UpdateSerialPort event
            advanceModeForm.StartPosition = FormStartPosition.Manual; // Set the form's start position
            advanceModeForm.Location = new System.Drawing.Point(this.Location.X + 50, this.Location.Y + 50); // Set form location relative to the current form
            advanceModeForm.ReceivedSerialPort(serialPort_Current); // Pass the current serial port info to the new form
            advanceModeForm.Show(); // Show the new form
        }

        /*
            This function handles the click event for a button (presumably a "Settings" button).
            When clicked, it opens a new form (setup) and passes necessary data to it while also subscribing to events for updating data.
         */
        private void SettingBTN_Click(object sender, EventArgs e)
        {
            // Create a new instance of the 'setup' form
            setup newform = new setup();

            // Subscribe to the 'WarningDistanceUpdated' event in the new form
            newform.WarningDistanceUpdated += UpdateInforWarning;

            // Set the starting position of the new form manually
            newform.StartPosition = FormStartPosition.Manual;

            // Set the location of the new form relative to the current form (50 pixels to the right and 50 pixels down)
            newform.Location = new System.Drawing.Point(this.Location.X + 50, this.Location.Y + 50);

            // Pass data (in this case, 'Data_Warning') to the new form by calling 'ReceiveData'
            newform.ReceiveData(Data_Warning);

            // Show the new form
            newform.Show();
        }

        // This function handles the event when data is received from the forwarder, processes the message, and updates queues accordingly.
        private void Forwader_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string buffer = null;
            try
            {
                // Read the incoming message from the forwarder
                buffer = Forwader.ReadLine();
            }
            catch (Exception ex)
            {
                // Handle any exceptions (no specific handling in this code)
            }

            if (buffer != null)
            {
                if (pingTimeout.IsRunning == true)
                {
                    pingTimeout.Stop(); // Stop the timeout timer if data is received
                }

                checkTimeout.Restart(); // Restart the timeout for checking the response
                parseMessage(buffer); // Parse the received message

                // If the response is not already processed, add it to the mapping data
                if (isResponExist(IdMappingData) != true)
                {
                    if (String.Compare(IdFilterResponse, ParseData.Id) == 0)
                    {
                        mappingData.Add(IdMappingData, buffer); // Add the message to mapping data
                        requestResponseQueue.Enqueue(new Message_t(ParseData.Id, ParseData.Message)); // Enqueue the message for processing
                    }
                }

                // If the data matches specific node IDs, enqueue the data for processing
                if (String.Compare(IdDataNode1, ParseData.Id) == 0 || String.Compare(IdDataNode2, ParseData.Id) == 0)
                {
                    dataReceivedQueue.Enqueue(new Message_t(ParseData.Id, ParseData.Message)); // Enqueue the received message
                }
            }
        }
    }
}
