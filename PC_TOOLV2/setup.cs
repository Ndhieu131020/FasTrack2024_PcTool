// Import required libraries for handling UI, image processing, serial communication, etc.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PC_TOOLV2
{
    // The 'setup' class represents a form for configuring distance and rotation settings.
    public partial class setup : Form
    {
        // Event that is triggered when the warning distance or rotation is updated.
        public event EventHandler<Information_t> WarningDistanceUpdated;

        // Object to store rotation and distance values.
        private Information_t data = new Information_t();

        // Method to receive data (distance and rotation) from outside and update the internal data object.
        public void ReceiveData(Information_t Data)
        {
            if (Data != null)
            {
                // If the incoming data is not null, update the internal data values.
                data.Rotaion = Data.Rotaion;
                data.Distance = Data.Distance;
            }
            else
            {
                // If no data is received, set rotation and distance to zero.
                data.Rotaion = 0;
                data.Distance = 0;
            }
        }

        // Constructor: Initializes the form and its components.
        public setup()
        {
            InitializeComponent();
        }

        // Event handler for button1 click: Displays the current distance in textBox1.
        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = data.Distance.ToString();
        }

        // Event handler for button2 click: Parses the values from textBox1 and textBox2, and updates the data.
        // If the rotation value exceeds 180, a warning message is shown.
        private void button2_Click(object sender, EventArgs e)
        {
            Information_t setup = new Information_t();
            // Try parsing the text values from textBox1 and textBox2 to integers.
            Int32.TryParse(textBox1.Text.ToString(), out setup.Distance);
            Int32.TryParse(textBox2.Text.ToString(), out setup.Rotaion);

            // Check if the rotation value exceeds the allowed threshold of 180.
            if (setup.Rotaion > 90)
            {
                // If so, show a warning message.
                MessageBox.Show("The entered value exceeds the allowed threshold.");
            }
            else
            {
                // Otherwise, invoke the WarningDistanceUpdated event with the updated values and close the form.
                WarningDistanceUpdated?.Invoke(this, setup);
                this.Close();
            }
        }

        // Event handler for button3 click: Displays the current rotation value in textBox2.
        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.Text = data.Rotaion.ToString();
        }

        // Event handler for form load: Initializes the text boxes with the current values of distance and rotation.
        private void setup_Load(object sender, EventArgs e)
        {
            textBox1.Text = data.Distance.ToString();
            textBox2.Text = data.Rotaion.ToString();
        }
    }
}
