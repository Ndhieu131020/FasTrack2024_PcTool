using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PC_TOOLV2
{
    // Enum to represent different states of the PC tool
    public enum PCToolState_t
    {
        PCTool_Deinit,       // State when the PC tool is deinitialized
        PCTool_Init,         // State when the PC tool is initialized
        PCTool_Running,      // State when the PC tool is actively running
        PCTool_Pause,        // State when the PC tool is paused
        PCTool_Reconnect,    // State when the PC tool is attempting to reconnect
        PCTool_Disconnected  // State when the PC tool is disconnected
    }

    // Enum to represent the state of the serial communication
    public enum SerialState_t
    {
        Serial_Idle,         // Serial is idle and not performing any operation
        Serial_Sending,      // Serial is in the process of sending data
        Serial_Receiving,    // Serial is in the process of receiving data
        Serial_Waiting,      // Serial is waiting for a response or data
        Serial_Received      // Serial has successfully received data
    }

    // Class representing a message with an ID and content
    public class Message_t
    {
        public string Id;        // Unique identifier for the message
        public string Message;   // Content of the message

        // Default constructor initializing the message with default values
        public Message_t()
        {
        }

        // Parameterized constructor for initializing with specific values
        public Message_t(string ID, string Message)
        {
            this.Id = ID;             // Assigns the provided ID to the message
            this.Message = Message;   // Assigns the provided content to the message
        }
    }

    // Class representing information about the serial port
    public class SerialPortInfor_t
    {
        public string PortName;   // Name of the serial port (e.g., "COM1")
        public int Baudrate;      // Communication baud rate for the serial port

        // Default constructor initializing with default values
        public SerialPortInfor_t()
        {

        }

        // Parameterized constructor for initializing with specific values
        public SerialPortInfor_t(string PortName, int Baudrate)
        {
            this.PortName = PortName;  // Assigns the provided port name
            this.Baudrate = Baudrate;  // Assigns the provided baud rate
        }
    }

    // Class representing additional information such as rotation and distance
    public class Information_t
    {
        public int Rotaion;   // Represents the angle of rotation
        public int Distance;  // Represents the distance traveled

        // Default constructor initializing rotation and distance to 0
        public Information_t()
        {
            Rotaion = 0;      // Default rotation value is 0
            Distance = 0;     // Default distance value is 0
        }

        // Parameterized constructor for initializing with specific values
        public Information_t(int Rotation, int Distance)
        {
            this.Rotaion = Rotation;  // Assigns the provided rotation value
            this.Distance = Distance; // Assigns the provided distance value
        }
    }

}
