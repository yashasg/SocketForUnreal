using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;

namespace BoggleClient
{
    public partial class BoggleWindow : Form
    {
        public BoggleWindow()
        {
            InitializeComponent();
            InitialState();
        }

        private void InitialState ()
        {
            RegisterUserButton.Enabled = true;
            JoinGameButton.Enabled = false;
            QuitGameButton.Enabled = false;
            PlayerBox.Enabled = true;
            ServerBox.Enabled = true;
            TimeBox.Enabled = false;
            WordBox.Enabled = false;
            Waiting.Visible = false;
            PlayerBox.Focus();
        }

        private void RegisteredState()
        {
            RegisterUserButton.Enabled = true;
            JoinGameButton.Enabled = true;
            QuitGameButton.Enabled = false;
            PlayerBox.Enabled = true;
            ServerBox.Enabled = true;
            TimeBox.Enabled = true;
            WordBox.Enabled = false;
            WordBox.Text = "";
            Waiting.Visible = false;
            JoinGameButton.Text = "Join Game";
            TimeBox.Focus();
        }

        private void JoiningState()
        {
            RegisterUserButton.Enabled = false;
            JoinGameButton.Enabled = true;
            QuitGameButton.Enabled = false;
            PlayerBox.Enabled = false;
            ServerBox.Enabled = false;
            TimeBox.Enabled = false;
            WordBox.Enabled = false;
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += WaitingForGame;
            timer.Start();
        }

        private void PlayingState()
        {
            RegisterUserButton.Enabled = false;
            JoinGameButton.Enabled = false;
            QuitGameButton.Enabled = true;
            PlayerBox.Enabled = false;
            ServerBox.Enabled = false;
            TimeBox.Enabled = false;
            WordBox.Text = "";
            WordBox.Enabled = true;
            WordBox.Focus();
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += UpdateScore;
            timer.Start();
        }

        private async void WaitingForGame (object o, EventArgs e)
        {
            dynamic result = await client.DoGetAsync("games/" + GameID);

            if (result is HttpStatusCode)
            {
                MessageBox.Show("Error: " + result.ToString());
            }
            else if (result.GameState != "pending")
            {
                timer.Stop();
                string board = result.Board;
                for (int r = 0; r < 4; r++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        string s = (board[4 * r + c] == 'Q') ? "Qu" : board[4 * r + c].ToString();
                        Grid.GetControlFromPosition(c, r).Text = s;
                    }
                }
                Time.Text = result.TimeLeft;
                Player1.Text = result.Player1.Nickname;
                Score1.Text = result.Player1.Score.ToString();
                Player2.Text = result.Player2.Nickname;
                Score2.Text = result.Player2.Score.ToString();
                Words1.Text = "";
                Words2.Text = "";
                Waiting.Visible = false;
                PlayingState();
            }
        }

        private async void UpdateScore(object o, EventArgs e)
        {
            dynamic result = await client.DoGetAsync("games/" + GameID, "yes");

            if (result is HttpStatusCode)
            {
                MessageBox.Show("Error: " + result.ToString());
            }
            else if (result.GameState == "active")
            {
                Time.Text = result.TimeLeft;
                Score1.Text = result.Player1.Score.ToString();
                Score2.Text = result.Player2.Score.ToString();
            }
            else if (result.GameState == "completed")
            {
                timer.Stop();
                Time.Text = result.TimeLeft;
                Score1.Text = result.Player1.Score.ToString();
                Score2.Text = result.Player2.Score.ToString();
                foreach (dynamic d in result.Player1.WordsPlayed)
                {
                    Words1.AppendText(d.Word.ToString() + "   " + d.Score.ToString() + "\n");
                }
                foreach (dynamic d in result.Player2.WordsPlayed)
                {
                    Words2.AppendText(d.Word.ToString() + "   " + d.Score.ToString() + "\n");
                }
                RegisteredState();
            }
        }

        private string PlayerToken
        {
            get; set;
        }

        private string GameID
        {
            get; set;
        }

        private RestClient client;

        private Timer timer;

        public void Error (string s)
        {
            MessageBox.Show(s);
        }

        private async void RegisterUserButton_Click(object sender, EventArgs e)
        {
            try
            {
                client = new RestClient(ServerBox.Text);
            }
            catch (UriFormatException)
            {
                MessageBox.Show("Error: Bad client address");
                return;
            }

            if (PlayerBox.Text.Trim() == "")
            {
                MessageBox.Show("Error: No user name provided");
                return;
            }

            Waiting.Visible = true;
            RegisterUserButton.Text = "Cancel Register User";
            dynamic data = new ExpandoObject();
            data.Nickname = PlayerBox.Text;
            dynamic result = await client.DoPostAsync(data, "users");
            RegisterUserButton.Text = "Register User";
            Waiting.Visible = false;
            if (result is HttpStatusCode)
            {
                MessageBox.Show("Error: " + result.ToString());
            }
            else
            {
                PlayerToken = result.UserToken;
                RegisteredState();               
            }
        }

        private async void JoinGameButton_Click(object sender, EventArgs e)
        {
            if (JoinGameButton.Text.StartsWith("Cancel"))
            {
                timer.Stop();
                Waiting.Visible = false;
                RegisteredState();
                return;
            }

            Waiting.Visible = true;
            JoinGameButton.Text = "Cancel Join Game";
            dynamic data = new ExpandoObject();
            data.UserToken = PlayerToken;
            data.TimeLimit = Int32.Parse(TimeBox.Text);
            dynamic result = await client.DoPostAsync(data, "games");
            if (result is HttpStatusCode)
            {
                Waiting.Visible = false;
                MessageBox.Show("Error: " + result.ToString());
            }
            else
            {
                GameID = result.GameID;
                JoiningState();
            }
        }

        private async void WordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string word = WordBox.Text;
                WordBox.Text = "";
                dynamic data = new ExpandoObject();
                data.Word = word;
                data.UserToken = PlayerToken;
                dynamic result = await client.DoPutAsync(data, "games/" + GameID);
                if (result is HttpStatusCode)
                {
                    MessageBox.Show("Error: " + result.ToString());
                }
            }
        }

        private void QuitGameButton_Click(object sender, EventArgs e)
        {
            timer.Stop();
            RegisteredState();
        }
    }
}
