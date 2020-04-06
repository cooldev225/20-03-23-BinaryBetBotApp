using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;

using Newtonsoft.Json;
using WebSocket4Net;
using SuperSocket.ClientEngine;
using System.Web.Script.Serialization;
using OddEvenBotApp.Model;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Text.RegularExpressions;
using Tulpep.NotificationWindow;
using System.Threading;

namespace OddEvenBotApp
//https://forum.unity.com/threads/solved-need-help-to-open-websocket-connection-with-wss-string.492611/
{
    public partial class MainForm : Form
    {
        /* Websocket for binary.com */
        private WebSocket BinaryNetSocket;
        /* Websocket connection string */
        private string websocketURI = "wss://ws.binaryws.com/websockets/v3?app_id=_APPID_";
        /* Authorisation string */
        private string authorization = "{\"authorize\":\"_TOKEN_\"}";
        /* String for Account Info and Trading */
        private string balance_request = "{\"balance\":\"1\",\"subscribe\":\"1\"}"; // Balance subscription to automated get changes
        private string portfolio_request = "{\"portfolio\":\"1\"}";
        private string profittable_request = "{\"profit_table\":\"1\",\"description\":\"1\",\"limit\":\"20\"}";
        private string statement_request = "{\"statement\": 1,\"description\": 1,\"limit\": 100,\"offset\": 25}";
        private string ticks_request = "{\"ticks\": \"_MARKET_NAME_\",\"subscribe\": 1}";
        private string price_proposal_request = "{\"proposal\": 1,\"amount\": _PAYAMOUNT_,\"basis\": \"stake\",\"contract_type\": \"DIGIT_ODDEVEN_\",\"currency\": \"USD\",\"duration\": 1,\"duration_unit\": \"t\",\"symbol\": \"_MARKET_NAME_\"}";
        private string buy_contract_request = "{\"buy\": \"_PROPOSALID_\",\"price\": _PAYAMOUNT_}";
        private string search_transaction_request = "{\"statement\": 1,\"description\": 1}";//,\"date_from\":\"_STARTDATE_\",\"limit\": 100,\"action_type\": \"buy\"
        private string search_profit_request = "{\"profit_table\": 1,\"description\": 1,\"limit\": 50,\"offset\": 0,\"sort\": \"DESC\"}";//\"limit\": 100,\"offset\": 0,////"limit": 25,  "offset": 25,  "sort": "ASC"
        //---------------------------------------------//
        private double _start_balance = 0.0;
        private double _current_balance = 0.0;
        private double _target_balance = 0.0;
        private double _stoploss_balance = 0.0;
        private string _country = "en";
        private string _currency = "USD";
        private string _email = "";
        private string _fullname = "";
        private int _is_virtual = 1;
        /*************************start params*****************************/
        private string[] _Param_Markets;
        private int[] _Param_OnOff;
        private int[] _Param_SequenceTrigger;
        private int[] _Param_BuyReserve;
        private double[] _Param_DefaultStake;
        private double[] _Param_MGMultiplier;
        private double[] _Param_NextStakeDueMG;
        private int[] _Param_MGTriesBeforeBreak;
        private int[] _Param_LossBreakeTicks;
        private int _market_cnt = 0;
        /*************************end   params*****************************/
        private int[] _exe_sequence_Markets;
        private bool[] _exe_tick_started;
        private int[] _exe_sequence_loss_count;
        private int[] _exe_lastdigit_Markets;
        private int[] _exe_mg_break_ticks;
        private int[] _exe_mg_break_waiting_ticks;
        private string[] _exe_tick_log_market;
        private bool[] _exe_mg_lock;
        private int[][] _exe_trigger_count_Markets=new int[2][];
        private char[] _odd_even = new char[2]{ 'E', 'O' };
        private int[] _exe_lost_Markets;
        private int[] _last_win_loss_status;
        private int _last_digit_row_index = 9;
        private int _even_odd_sequence_row_index = 10;
        private int _trigger_cnt_row_index = 12;
        private int _max_loss_row_index = 13;
        private int _win_count = 0;
        private int _loss_count = 0;
        private double _win_amount = 0;
        private double _loss_amount = 0;
        private StateMentRow _statement_last_row=new StateMentRow();
        private Queue<ContractParam> _exe_bet_queue = new Queue<ContractParam>();
        private Queue<string> _exe_proposal_queue = new Queue<string>();
        private Stack<ContractParam> _exe_bet_stack = new Stack<ContractParam>();
        private System.Media.SoundPlayer player = new System.Media.SoundPlayer();

        private int _stop_ = 0;
        //private System.IO.StreamWriter _buy_log_file_writer_ = new System.IO.StreamWriter("buy_log.csv");
        private string _buy_log_file_body_ = "";//balance_after,buy_price,contract_id,payout,purchase_time,transaction_id,price\n";
        private int _w = 948;
        private int _h = 600;
        private bool is_connected = false;
        private bool _alert_confirm = false;
        private string _account_info_ = "";

        delegate void SetInfoTextCallback();
        delegate void BuyAfterProcessCallBack();//(double balance, double betprice, double getprice, string timestamp, string transid, string shortcode);
        public MainForm()
        {
            InitializeComponent();
            this.websocketURI = this.websocketURI.Replace("_APPID_", ConfigurationManager.AppSettings["AppID"]);
            this.authorization = this.authorization.Replace("_TOKEN_", ConfigurationManager.AppSettings["Token"]);
            this.token_txt.Text = ConfigurationManager.AppSettings["Token"];
            this.app_id_txt.Text = ConfigurationManager.AppSettings["AppID"];
            BinaryNetSocket = new WebSocket(websocketURI);
            BinaryNetSocket.Closed += new EventHandler(websocket_Closed);
            BinaryNetSocket.Error += new EventHandler<ErrorEventArgs>(websocket_Error);
            BinaryNetSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);
            BinaryNetSocket.Opened += new EventHandler(websocket_Opened);
            BinaryNetSocket.Open();

            this.Size = new Size(_w, _h);
            displayStaticVariables();
            displayDynamicVariables();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
        private void displayStaticVariables()
        {
            int i,cols;
            string[] arr;
            DataGridViewRow row;
            this.title_lab.Text = ConfigurationManager.AppSettings["ApplicationName"];
            this.version_lab.Text = ConfigurationManager.AppSettings["Version"];
            this.copyright_lab.Text = ConfigurationManager.AppSettings["CopyRight"];
            double.TryParse(ConfigurationManager.AppSettings["TargetBalance"],out this._target_balance);
            double.TryParse(ConfigurationManager.AppSettings["StopLossBalance"],out this._stoploss_balance);
            this.status_account_val_lab.Text= "VRTC"+ ConfigurationManager.AppSettings["AppID"].Substring(0, ConfigurationManager.AppSettings["AppID"].Length-4)+"xxx";
            this.info_target_val.Text = $"${this._target_balance}";
            this.info_stop_val.Text = $"${this._stoploss_balance}";
            this._Param_Markets= ConfigurationManager.AppSettings["Param_Markets"].Split(',');
            this._market_cnt = this._Param_Markets.Length;
            this._Param_OnOff =new int[this._market_cnt];
            this._Param_SequenceTrigger = new int[this._market_cnt];
            this._Param_BuyReserve = new int[this._market_cnt];
            this._Param_DefaultStake = new double[this._market_cnt];
            this._Param_MGMultiplier = new double[this._market_cnt];
            this._Param_NextStakeDueMG = new double[this._market_cnt];
            this._Param_MGTriesBeforeBreak = new int[this._market_cnt];
            this._Param_LossBreakeTicks = new int[this._market_cnt];
            for (i = 0, arr = ConfigurationManager.AppSettings["Param_OnOff"].Split(','); i < arr.Length; int.TryParse(arr[i], out this._Param_OnOff[i]), i++) ;
            for (i = 0, arr = ConfigurationManager.AppSettings["Param_SequenceTrigger"].Split(','); i < arr.Length; int.TryParse(arr[i], out this._Param_SequenceTrigger[i]), i++) ;
            for (i = 0, arr = ConfigurationManager.AppSettings["Param_BuyReserve"].Split(','); i < arr.Length; int.TryParse(arr[i], out this._Param_BuyReserve[i]), i++) ;
            for (i = 0, arr = ConfigurationManager.AppSettings["Param_DefaultStake"].Split(','); i < arr.Length; double.TryParse(arr[i], out this._Param_DefaultStake[i]), i++) ;
            for (i = 0, arr = ConfigurationManager.AppSettings["Param_MGMultiplier"].Split(','); i < arr.Length; double.TryParse(arr[i], out this._Param_MGMultiplier[i]), i++) ;
            for (i = 0, arr = ConfigurationManager.AppSettings["Param_NextStakeDueMG"].Split(','); i < arr.Length; double.TryParse(arr[i], out this._Param_NextStakeDueMG[i]), this._Param_NextStakeDueMG[i]=0, i++) ;
            for (i = 0, arr = ConfigurationManager.AppSettings["Param_MGTriesBeforeBreak"].Split(','); i < arr.Length; int.TryParse(arr[i], out this._Param_MGTriesBeforeBreak[i]), i++) ;
            for (i = 0, arr = ConfigurationManager.AppSettings["Param_LossBreakeTicks"].Split(','); i < arr.Length; int.TryParse(arr[i], out this._Param_LossBreakeTicks[i]), i++) ;

            this._exe_sequence_Markets = new int[this._market_cnt];
            this._exe_lastdigit_Markets = new int[this._market_cnt];
            this._exe_trigger_count_Markets[0] = new int[this._market_cnt];
            this._exe_trigger_count_Markets[1] = new int[this._market_cnt];
            this._exe_sequence_loss_count = new int[this._market_cnt];
            this._exe_lost_Markets = new int[this._market_cnt];
            this._exe_mg_break_ticks = new int[this._market_cnt]; 
            this._last_win_loss_status = new int[this._market_cnt]; 
            this._exe_tick_log_market = new string[this._market_cnt]; 
            this._exe_tick_started = new bool[this._market_cnt];
            this._exe_mg_break_waiting_ticks = new int[this._market_cnt];
            this._exe_mg_lock = new bool[this._market_cnt]; 
            for (i = 0; i < this._market_cnt; i++) {
                this._exe_sequence_Markets[i] = 0;
                this._exe_lastdigit_Markets[i] = -1;
                this._exe_trigger_count_Markets[0][i] = 0;
                this._exe_trigger_count_Markets[1][i] = 0;
                this._exe_lost_Markets[i] = 0;
                this._exe_sequence_loss_count[i] = 0;
                this._last_win_loss_status[i] = -1;
                this._exe_tick_log_market[i] = "digit=";
                this._exe_tick_started[i] = false;
                this._exe_mg_break_ticks[i] = -1;//initial MG Level will be 0 in every market
                this._exe_mg_break_waiting_ticks[i] = 0;
                this._exe_mg_lock[i] = false;
            }

            DataGridViewColumn[] param_grid_columns = new DataGridViewColumn[cols= this._market_cnt + 2];
            for (i = 0; i < cols; i++)
            {
                param_grid_columns[i] = new DataGridViewColumn();
                if (i == 0)
                {
                    param_grid_columns[i].HeaderText = "Input Parameters";
                    param_grid_columns[i].Width = 125;
                    param_grid_columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    param_grid_columns[i].ReadOnly = true;
                }
                else if (i == 1)
                {
                    param_grid_columns[i].HeaderText = "ALL";
                    param_grid_columns[i].Width = 40;
                    param_grid_columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    param_grid_columns[i].ReadOnly = false;
                }
                else {
                    param_grid_columns[i].HeaderText = this._Param_Markets[i - 2].Replace("RD","");
                    param_grid_columns[i].Width = 51;
                    param_grid_columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    param_grid_columns[i].ReadOnly = false;
                }
                this.param_grid.Columns.Add(param_grid_columns[i]);
            }
            Color alter_c = Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            for (i = 0, row = new DataGridViewRow(), row.DefaultCellStyle.BackColor = Color.White , row.DefaultCellStyle.SelectionBackColor = Color.White, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "On/Off" });
                else if (i == 1) row.Cells.Add(new DataGridViewCheckBoxCell { Value = 0 });
                else row.Cells.Add(new DataGridViewCheckBoxCell { Value = this._Param_OnOff[i - 2] });
            }
            
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(),row.DefaultCellStyle.BackColor = alter_c, row.DefaultCellStyle.SelectionBackColor = alter_c, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "Sequence Trigger" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = this._Param_SequenceTrigger[i - 2] });
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(), row.DefaultCellStyle.BackColor = Color.White, row.DefaultCellStyle.SelectionBackColor = Color.White, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "Buy Reserve?" });
                else if (i == 1) row.Cells.Add(new DataGridViewCheckBoxCell { Value = 0 });
                else row.Cells.Add(new DataGridViewCheckBoxCell { Value = this._Param_BuyReserve[i - 2] });
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(),row.DefaultCellStyle.BackColor = alter_c, row.DefaultCellStyle.SelectionBackColor = alter_c, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "Default Stake" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = this._Param_DefaultStake[i - 2] });
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(), row.DefaultCellStyle.BackColor = Color.White, row.DefaultCellStyle.SelectionBackColor = Color.White, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "MG Multipuliar" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = this._Param_MGMultiplier[i - 2] });
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(), row.DefaultCellStyle.BackColor = alter_c, row.DefaultCellStyle.SelectionBackColor = alter_c, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "Next Stake due MG" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = this._Param_NextStakeDueMG[i - 2] });
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(), row.DefaultCellStyle.BackColor = Color.White, row.DefaultCellStyle.SelectionBackColor = Color.White, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "MG Tries before Break" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = this._Param_MGTriesBeforeBreak[i - 2] });
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(), row.DefaultCellStyle.BackColor = alter_c, row.DefaultCellStyle.SelectionBackColor = alter_c, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "Loss Break (Ticks)" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = this._Param_LossBreakeTicks[i - 2] });
            }
            this.param_grid.Rows.Add(row);

            for (i = 0, row = new DataGridViewRow(),row.Height=45, row.DefaultCellStyle.Font= new Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0))), row.DefaultCellStyle.ForeColor=Color.White,row.DefaultCellStyle.BackColor = this.param_grid.ColumnHeadersDefaultCellStyle.BackColor, row.DefaultCellStyle.SelectionBackColor = this.param_grid.ColumnHeadersDefaultCellStyle.BackColor, row.DefaultCellStyle.SelectionForeColor = Color.White; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "EXECUTION RESULTS" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = ""});
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = "" });
                //row.Cells[0].Style.BackColor = Color.Red;
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(), row.DefaultCellStyle.BackColor = alter_c, row.DefaultCellStyle.SelectionBackColor = alter_c, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "Last Digits" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "-" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = "-" });
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(), row.DefaultCellStyle.BackColor = Color.White, row.DefaultCellStyle.SelectionBackColor = Color.White, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "Even Sequences" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "0" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = "0" });
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(),row.DefaultCellStyle.BackColor = alter_c, row.DefaultCellStyle.SelectionBackColor = alter_c, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "Odd Sequences" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "0" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = "0" });
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(), row.DefaultCellStyle.BackColor = Color.White, row.DefaultCellStyle.SelectionBackColor = Color.White, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "Max Sequence" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "0" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = "0" });
            }
            this.param_grid.Rows.Add(row);
            for (i = 0, row = new DataGridViewRow(), row.DefaultCellStyle.BackColor = alter_c, row.DefaultCellStyle.SelectionBackColor = alter_c, row.DefaultCellStyle.SelectionForeColor = Color.Black; i < cols; i++)
            {
                if (i == 0) row.Cells.Add(new DataGridViewTextBoxCell { Value = "Max Consecutive Loss" });
                else if (i == 1) row.Cells.Add(new DataGridViewTextBoxCell { Value = "0" });
                else row.Cells.Add(new DataGridViewTextBoxCell { Value = "0" });
            }
            this.param_grid.Rows.Add(row);

            //row = (DataGridViewRow)this.param_grid.Rows[0].Clone();
            this.status_time_val_lab.Text = "00:00:00";
            foreach(DataGridViewRow r in this.param_grid.Rows)
            {
                r.Height = 24;
            }
        }
        private void displayDynamicVariables()
        {
            if (this.info_start_val.InvokeRequired)
            {
                SetInfoTextCallback d = new SetInfoTextCallback(displayDynamicVariables);
                this.Invoke(d);
            }
            else
            {
                this.info_start_val.Text = $"${Math.Round(this._start_balance,2)}";
                this.info_current_val.Text = $"${Math.Round(this._current_balance,2)}";
                if (this._current_balance > this._start_balance)
                {
                    this.info_percentage_val.Text = $"${Math.Round(this._current_balance - this._start_balance, 2)}";
                    this.info_percentage_loss_val.Text = $"$0";
                }
                else {
                    this.info_percentage_val.Text = $"$0";
                    this.info_percentage_loss_val.Text = $"${Math.Round(this._start_balance - this._current_balance, 2)}";
                }
            }
        }
        private void websocket_Opened(object sender, EventArgs e)
        {
            is_connected = true;
            //MessageBox.Show("Send Authentication");
            sendMessageAPI(authorization);
            //MessageBox.Show("!!!Connected!!!");
        }

        private void websocket_Error(object sender, ErrorEventArgs e)
        {
            MessageBox.Show("Error: " + e.Exception.Message);
        }

        private void websocket_Closed(object sender, EventArgs e)
        {
           MessageBox.Show("Connection Closed");
            is_connected = false;
            ///BinaryNetSocket.Open();
        }
        private int getIndexOfMarketName(string s) { if (s == null || s.Equals("")) return 0; for (int i = 0; i < this._market_cnt; i++) if (this._Param_Markets[i].Equals(s)) return i;return 0; }
        private int getIndexOfShortcode(string s)  {if (s == null || s.Equals("")) return 0; for (int i = 0; i < this._market_cnt; i++)if (s.IndexOf($"_{this._Param_Markets[i]}_")>-1) return i; return 0; }
        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            
            var msg = JsonConvert.DeserializeObject<JObject>(e.Message);
            string msgType = msg.Value<string>("msg_type");
            
            switch (msgType)
            {
                case "balance":
                    sendMessageAPI(this.portfolio_request);
                    
                    break;
                case "authorize":
                    /*
                     * "authorize": {
                        "account_list": [
                          {
                            "currency": "",
                            "is_disabled": 0,
                            "is_virtual": 0,
                            "landing_company_name": "malta",
                            "loginid": "MLT93784"
                          },
                     */
                    var acc=msg.Value<JObject>("authorize").Value<JArray>("account_list").ToObject<List<JObject>>();
                    foreach (JObject v in acc)
                    {
                        this._account_info_ = v.Value<string>("loginid").Substring(0, v.Value<string>("loginid").Length - 4)+"xxx";
                    }
                    DisplayAccountInfo();
                    double.TryParse(msg.Value<JObject>("authorize").Value<string>("balance"), out this._start_balance);
                    this._current_balance = this._start_balance;
                    this._country = msg.Value<JObject>("authorize").Value<string>("country");
                    this._currency = msg.Value<JObject>("authorize").Value<string>("currency");
                    this._email = msg.Value<JObject>("authorize").Value<string>("email");
                    this._fullname = msg.Value<JObject>("authorize").Value<string>("fullname");
                    int.TryParse(msg.Value<JObject>("authorize").Value<string>("is_virtual"),out this._is_virtual);
                    displayDynamicVariables();
                    this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},authorize, , , , \n";
                    for (int i = 0; i < this._market_cnt; i++)
                    {
                        //if (this._Param_OnOff[i] > 0)
                        {
                            sendMessageAPI(this.ticks_request.Replace("_MARKET_NAME_", this._Param_Markets[i]));
                            this._exe_tick_started[i] = true;
                        }
                        //this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},tick {this._Param_Markets[i]} request, , , , \n";
                    }
                    //sendMessageAPI(this.balance_request);
                    break;
                case "portfolio":
                    //sendMessageAPI(this.statement_request);
                    break;
                case "tick":
                    foreach (JProperty property in msg.Properties())
                    {
                        if (property.Name.Equals("error")) {
                            //MessageBox.Show(property.Value.ToString());
                            return;
                        }
                    }
                    SolveTickBot(msg.Value<JObject>("tick").Value<string>("symbol"), int.Parse(Regex.Match(msg.Value<JObject>("tick").Value<string>("quote"), @"(.{1})\s*$").Value), msg.Value<JObject>("tick").Value<string>("epoch"));
                    break; 
                case "proposal":
                    /*
                      "proposal": {
                        "ask_price": 0.5,
                        "date_start": 1585364095,
                        "display_value": "0.50",
                        "id": "5f09c249-8440-3f9a-ddf0-911df2a55f9b",
                        "longcode": "Win payout if the last digit of Volatility 50 Index is odd after 1 ticks.",
                        "payout": 0.96,
                        "spot": 230.1122,
                        "spot_time": 1585364094
                      }
                        }**/
                    //MessageBox.Show(e.Message);
                    foreach (JProperty property in msg.Properties())
                    {
                        if (property.Name.Equals("error"))
                        {
                            //MessageBox.Show(property.Value.ToString());
                            return;
                        }
                    }
                    //this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},buy_contract_request,proposal_id={msg.Value<JObject>("proposal").Value<string>("id")},price={msg.Value<JObject>("proposal").Value<string>("payout")}, , \n";
                    sendMessageAPI(this.buy_contract_request.Replace("_PROPOSALID_", msg.Value<JObject>("proposal").Value<string>("id")).Replace("_PAYAMOUNT_", msg.Value<JObject>("proposal").Value<string>("payout")));
                    break;
                case "buy":
                    /*
                     "buy": {
                        "balance_after": 10013.86,
                        "buy_price": 0.5,
                        "contract_id": 77115797328,
                        "longcode": "Win payout if the last digit of Volatility 50 Index is odd after 1 ticks.",
                        "payout": 0.96,
                        "purchase_time": 1585364183,
                        "shortcode": "DIGITODD_R_50_0.96_1585364183_1T",
                        "start_time": 1585364183,
                        "transaction_id": 154166448048
                      },
                      "msg_type": "buy"
                     */
                    foreach (JProperty property in msg.Properties())
                    {
                        if (property.Name.Equals("error"))
                        {
                            return;
                        }
                    }

                    double.TryParse(msg.Value<JObject>("buy").Value<string>("balance_after"), out this._current_balance);
                    //this._exe_bet_queue.Enqueue(new ContractParam(msg.Value<JObject>("buy").Value<string>("contract_id"), msg.Value<JObject>("buy").Value<string>("shortcode"), msg.Value<JObject>("buy").Value<string>("transaction_id")));
                    //this._exe_bet_stack.Push(new ContractParam(msg.Value<JObject>("buy").Value<string>("contract_id"), msg.Value<JObject>("buy").Value<string>("shortcode"), msg.Value<JObject>("buy").Value<string>("transaction_id")));
                    this._exe_bet_queue.Enqueue(new ContractParam(msg.Value<JObject>("buy").Value<string>("contract_id"), double.Parse(msg.Value<JObject>("buy").Value<string>("buy_price")), msg.Value<JObject>("buy").Value<string>("purchase_time")));
                    //DisplayBalanceProcess_UI();
                    //this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},search_profit_request,{msg.Value<JObject>("buy").Value<string>("balance_after")},{msg.Value<JObject>("buy").Value<string>("contract_id")},time={msg.Value<JObject>("buy").Value<string>("purchase_time")},transaction_id={msg.Value<JObject>("buy").Value<string>("transaction_id")}\n";
                    //sendMessageAPI(this.search_profit_request);
                    if (this.mode_silent_chk.Checked == false)
                    {
                        player.SoundLocation = $"buy.wav";
                        player.Play();
                    }
                    sendMessageAPI(this.search_transaction_request.Replace("_STARTDATE_", msg.Value<JObject>("buy").Value<string>("purchase_time")));
                    this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},buy statement "+new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(double.Parse(msg.Value<JObject>("buy").Value<string>("purchase_time"))).ToString(); ;
                    break;
                case "sell":
                    double.TryParse(msg.Value<JObject>("sell").Value<string>("balance_after"), out this._current_balance);
                    DisplayBalanceProcess_UI();
                    break;
                case "profit_table":
                    break;
                case "statement":
                    /*
                     "msg_type": "statement",
                      "statement": {
                        "count": 2,
                        "transactions": [
                          {
                            "action_type": "sell",
                            "amount": 1.96,
                            "app_id": 22272,
                            "balance_after": 9426.87,
                            "contract_id": 77551043748,
                            "longcode": "Win payout if the last digit of Volatility 10 Index is even after 1 ticks.",
                            "payout": 1.96,
                            "purchase_time": 1585708464,
                            "reference_id": 155032456528,
                            "shortcode": "DIGITEVEN_R_10_1.96_1585708464_1T",
                            "transaction_id": 155032464828,
                            "transaction_time": 1585708467
                          },
                      }*/

                    foreach (JProperty property in msg.Properties())
                    {
                        if (property.Name.Equals("error"))
                        {
                            if (!this._alert_confirm)
                            {
                                //MessageBox.Show("Sorry, Your token is expired!\n" + property.Value.ToString());
                                //this._stop_ = 1 - this._stop_;
                                //if (this._stop_ > 0) this.stop_start_btn.Image = global::OddEvenBotApp.Properties.Resources.play_btn;
                                //else this.stop_start_btn.Image = global::OddEvenBotApp.Properties.Resources.stop_btn;
                                //this._alert_confirm = true;
                            }
                            return;
                        }
                    }
                    StatementAfterProcess(msg.Value<JObject>("statement").Value<JArray>("transactions").ToObject<List<JObject>>());
                    break;
                case "ping":
                    break;
                default:
                    break;
            }
        }
        private void SolveTickBot(string market,int tick,string timestamp) {
            if (this._exe_bet_queue.Count > 0)
            {
                sendMessageAPI(this.search_transaction_request.Replace("_STARTDATE_", this._exe_bet_queue.Peek().transaction_time));
                return;
            }
            if (this._stop_ == 1) return;
            int i = this.getIndexOfMarketName(market);

            int last_digit = tick % 2;
            
            
            //get current market index number , i=0 when r_10 because r_10 is first market
            if (this._Param_OnOff[i] == 0) return; //if r_10 is off,bot don't consider this market 
            if (this._exe_mg_break_ticks[i] > -1) {
                //this value is mg_break_ticks flag, if it is -1,this mean mg level is -1
                //this._last_win_loss_status[i]: this is last win/loss status of i-th market
                if (this._last_win_loss_status[i] == 0) this._exe_mg_break_ticks[i] = -1;
                else
                {//this is considered when last result is only loss
                    if(this._exe_sequence_Markets[i] >= this._Param_SequenceTrigger[i])
                    {//this check if current consequence count is bog than trigger parameter or not
                        //bot check if courrent tick value is same as last loss's tick or not
                        if (this._exe_lastdigit_Markets[i] == last_digit)
                        {
                            //in here, last_digit is current tick value
                            //also lastdigit_market[i] is i-th market's last loss's last digit
                            this._exe_mg_break_ticks[i]++;//increase current mg level
                            //if mg level > loss-break-mg-tick parameter 
                            if (this._exe_mg_break_ticks[i] > this._Param_MGTriesBeforeBreak[i])
                            {//in this case, bot wait next ticks as amount of LossBreakeTicks parameter
                                //if bot was waited ticks as amount of LossBreakeTicks parameter or not
                                if (_exe_mg_break_waiting_ticks[i] > this._Param_LossBreakeTicks[i])
                                {//if bot was waited ticks as amount of LossBreakeTicks parameter, bot stop waiting
                                    this._exe_mg_break_ticks[i] = -1;//stop wait and then go head below
                                    this._exe_mg_break_waiting_ticks[i] = 0;
                                }
                                else
                                {//if not, bot exit checking tick process to wait next tick
                                    _exe_mg_break_waiting_ticks[i]++;
                                    return;
                                }
                            }
                        }
                        else {
                            this._exe_mg_break_ticks[i] = -1;
                        }
                    }
                }
            }

            this._exe_tick_log_market[i] = this._exe_tick_log_market[i] + $"{last_digit}";
            if (this._exe_lastdigit_Markets[i] == last_digit) this._exe_sequence_Markets[i]++;
            else
            {
                this._exe_sequence_Markets[i] = 1;
                this._exe_lastdigit_Markets[i] = last_digit;
            }

            //this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},tick of {market},type={last_digit},{this._exe_tick_log_market[i]},{this._exe_sequence_loss_count[i]},\n";
            this.param_grid.Rows[this._last_digit_row_index].Cells[i + 2].Value = $"{this._odd_even[last_digit]}:{this._exe_sequence_Markets[i]}";

            //if ((this._exe_mg_level[i] >= this._Param_MGTriesBeforeBreak[i]&& this._exe_sequence_Markets[i] == this._Param_LossBreakeTicks[i]) ||(this._exe_mg_level[i] <= this._Param_MGTriesBeforeBreak[i]&&this._exe_sequence_Markets[i] == this._Param_SequenceTrigger[i])) {
            if (this._exe_sequence_Markets[i] >= this._Param_SequenceTrigger[i])
            {
                //int l = int.Parse(this.param_grid.Rows[5].Cells[i + 2].Value.ToString());
                if (this.insisent_mode_chk.Checked==false) this._exe_sequence_Markets[i] %= this._Param_SequenceTrigger[i];
                int ii = last_digit;
                //if (this._Param_BuyReserve[i] == 1) ii = 1 - ii;
                ii=this._even_odd_sequence_row_index + ii;
                this._exe_trigger_count_Markets[last_digit][i]++;
                this.param_grid.Rows[ii].Cells[i + 2].Value = this._exe_trigger_count_Markets[last_digit][i];
                this.param_grid.Rows[ii].Cells[1].Value = int.Parse(this.param_grid.Rows[ii].Cells[1].Value.ToString())+1;

                int maxit = 0,maxt=0;
                int.TryParse(this.param_grid.Rows[this._trigger_cnt_row_index].Cells[i + 2].Value.ToString(), out maxit);
                int.TryParse(this.param_grid.Rows[this._trigger_cnt_row_index].Cells[1].Value.ToString(), out maxt);
                if (this._exe_sequence_Markets[i] > maxit) this.param_grid.Rows[this._trigger_cnt_row_index].Cells[i + 2].Value = this._exe_sequence_Markets[i];
                if (this._exe_sequence_Markets[i] > maxt) this.param_grid.Rows[this._trigger_cnt_row_index].Cells[1].Value = this._exe_sequence_Markets[i];

                //this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},price_proposal_request,{market},{_exe_tick_log_market[i]},{getPayAmountInProposal(i)},{this._exe_sequence_Markets[i]} >= {this._Param_SequenceTrigger[i]}\n";
                //SendingBuyRequest(i,market);
                string req = this.price_proposal_request.Replace("_ODDEVEN_", this.getOddEvenInProposal(i)).Replace("_MARKET_NAME_", market);
                if(this._stop_==0)this._exe_proposal_queue.Enqueue(req);
                if (this._exe_bet_queue.Count==0&&this._exe_proposal_queue.Count == 1&&this._stop_==0)
                {
                    sendMessageAPI(this._exe_proposal_queue.Dequeue().Replace("_PAYAMOUNT_", this.getPayAmountInProposal(i).ToString()));
                    this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},tick proposal";
                }
            }

            //if (this._last_win_loss_status[i] == 1 && (this._Param_MGTriesBeforeBreak[i] == this._exe_sequence_Markets[i] - this._Param_SequenceTrigger[i])) {
            //    this._exe_sequence_Markets[i] = 0;
            //    if(this._Param_LossBreakeTicks[i]>0)this._exe_mg_break_ticks[i] = 0;
            //}
            //if (i == 0) DisplayTicksProcess_UI();
            
        }
        private void DisplayTicksProcess_UI()
        {
            if (this.info_current_val.InvokeRequired)
            {
                BuyAfterProcessCallBack d = DisplayTicksProcess_UI; //BuyAfterProcessCallBack(BuyAfterProcess);
                this.Invoke(d);
                return;
            }
             this.r10_tick_lab.Text = $"{this.r10_tick_lab.Text}{this._exe_lastdigit_Markets[0]}";
        }
        private void DisplayAccountInfo() {
            if (this.status_account_val_lab.InvokeRequired)
            {
                BuyAfterProcessCallBack d = DisplayAccountInfo; //BuyAfterProcessCallBack(BuyAfterProcess);
                this.Invoke(d);
                return;
            }
            this.status_account_val_lab.Text = this._account_info_;
        }
        private void DisplayBalanceProcess_UI()
        {
            if (this.info_current_val.InvokeRequired)
            {
                BuyAfterProcessCallBack d = DisplayBalanceProcess_UI; //BuyAfterProcessCallBack(BuyAfterProcess);
                this.Invoke(d);
                return;
            }
            this.info_current_val.Text = this._current_balance.ToString();
        }
        private void BuyAfterProcess_UI() {
            if (this.info_current_val.InvokeRequired)
            {
                BuyAfterProcessCallBack d = BuyAfterProcess_UI; //BuyAfterProcessCallBack(BuyAfterProcess);
                this.Invoke(d);
                return;
            }
            this.info_current_val.Text = Math.Round(this._current_balance,2).ToString();
            
            DataGridViewRow row = new DataGridViewRow();
            int r = this.statement_grid.Rows.Count;
            row.DefaultCellStyle.BackColor = Color.White;
            Color[] color = new Color[2];
            color[0] = Color.White;
            color[1] = Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            row.DefaultCellStyle.BackColor = color[r % 2];
            row.Cells.Add(new DataGridViewTextBoxCell { Value = this._statement_last_row.Timestamp });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = this._statement_last_row.Reference });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = this._statement_last_row.TradeType });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = this._statement_last_row.Market });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = this._statement_last_row.BuyPrice });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = this._statement_last_row.ProfitLoss });
            row.Cells.Add(new DataGridViewTextBoxCell { Value = this._statement_last_row.SortColumn });
            row.Cells[5].Style.ForeColor = double.Parse(this._statement_last_row.ProfitLoss)<0?Color.Red : Color.Blue;
            this.statement_grid.Rows.Add(row);
            this.statement_grid.Sort(this.statement_grid.Columns[6],ListSortDirection.Descending);

            var rVisible = this.statement_grid.Height / this.statement_grid.Rows[0].Height - 1;
            //if (this.statement_grid.FirstDisplayedScrollingRowIndex + rVisible < this.statement_grid.Rows.Count)
            //    this.statement_grid.FirstDisplayedScrollingRowIndex += 2;
            //else this.statement_grid.FirstDisplayedScrollingRowIndex = 0;
            row.Selected = true;
            //this.statement_grid.Rows[this.statement_grid.Rows.Count - 1].Selected = true;

            int i = this.getIndexOfMarketName(this._statement_last_row.Market);

            if (this.mode_martingale_chk.Checked == true)// && this._exe_mg_lock[i] == false)
                if (this._last_win_loss_status[i] == 1 && this._exe_sequence_loss_count[i] > 0)
                    this.param_grid.Rows[5].Cells[i + 2].Value = Math.Round( this._Param_DefaultStake[i]*Math.Pow(this._Param_MGMultiplier[i], this._exe_sequence_loss_count[i] ),2);
            if(this._last_win_loss_status[i]==0)
                    this.param_grid.Rows[5].Cells[i + 2].Value = "0";
            if (this._exe_sequence_loss_count[i] > int.Parse(this.param_grid.Rows[this._max_loss_row_index].Cells[i + 2].Value.ToString())) 
                this.param_grid.Rows[this._max_loss_row_index].Cells[i + 2].Value = this._exe_sequence_loss_count[i].ToString();
            if (this._exe_sequence_loss_count[i] > int.Parse(this.param_grid.Rows[this._max_loss_row_index].Cells[1].Value.ToString()))
                this.param_grid.Rows[this._max_loss_row_index].Cells[1].Value = this._exe_sequence_loss_count[i].ToString(); 
            
            this.status_winloss_val_lab.Text = $"{this._win_count}/{this._loss_count}";
            this.status_runs_val_lab.Text = $"{this._win_count+this._loss_count}";
            if (this._current_balance > this._start_balance)
            {
                this.info_percentage_val.Text = $"${Math.Round(this._current_balance - this._start_balance, 2)}";
                this.info_percentage_loss_val.Text = $"$0";
            }
            else
            {
                this.info_percentage_val.Text = $"$0";
                this.info_percentage_loss_val.Text = $"${Math.Round(this._start_balance - this._current_balance, 2)}";
            }
            this.status_totalprofit_val_lab.Text = $"{this._win_amount}";


            if (this._start_balance - this._current_balance >= this._stoploss_balance) {
                this._stop_ = 1;
                StartStopProcess();
                PopupNotifier popup = new PopupNotifier();
                popup.TitleText = "Binary Bet Bot Notification";
                popup.ContentText = $"Waning!!!\nYou lost ${this._loss_amount}.";
                popup.Popup();// show  
                player.SoundLocation = $"loss.wav";
                player.Play();
            }
            if (this._current_balance - this._start_balance >= this._target_balance) {
                this._stop_ = 1;
                StartStopProcess();
                PopupNotifier popup = new PopupNotifier();
                popup.TitleText = "Binary Bet Bot Notification";
                popup.ContentText = "Congratulation!!!\nYou got profit of target balance.";
                popup.Popup();// show  
                player.SoundLocation = $"coin.wav";
                player.Play();
            }
        }
        

        private double getPayAmountInProposal(int i)
        {
            double v = this._Param_DefaultStake[i];
            double l=double.Parse(this.param_grid.Rows[5].Cells[i+2].Value.ToString());
            if (l == 0) l = 1;
            if (this._exe_sequence_loss_count[i] > 0) v = l;
            /*double v = this._Param_DefaultStake[i];
            if (l > 0)
            {
                for(int j=0;j< l;j++)
                    v *= this._Param_MGMultiplier[i];
            }*/
            return v;
        }
        private void StatementAfterProcess_UI() {
            if (this.statement_grid.InvokeRequired)
            {
                BuyAfterProcessCallBack d = StatementAfterProcess_UI;
                this.Invoke(d);
                return;
            }
            for (int i = this.statement_grid.Rows.Count-1,j; i >= 0; i--) {
                if (this.statement_grid.Rows[i].Cells["reference"].Value.ToString().Equals(this._statement_last_row.Reference)) {
                    this.statement_grid.Rows[i].Cells["profitloss"].Value = this._statement_last_row.ProfitLoss;
                    this.param_grid.Rows[this._max_loss_row_index].Cells[(j = this.getIndexOfMarketName(this._statement_last_row.Market))+2].Value = this._exe_lost_Markets[j].ToString();
                    if(int.Parse(this.param_grid.Rows[this._max_loss_row_index].Cells[1].Value.ToString())< this._exe_lost_Markets[j]) this.param_grid.Rows[this._max_loss_row_index].Cells[1].Value= this._exe_lost_Markets[j].ToString();
                    return;
                }
            }
        }
        private void StatementAfterProcess(List<JObject> transactions) {
            /*
             * {
                            "action_type": "sell",
                            "amount": 1.96,
                            "app_id": 22272,
                            "balance_after": 9426.87,
                            "contract_id": 77551043748,
                            "longcode": "Win payout if the last digit of Volatility 10 Index is even after 1 ticks.",
                            "payout": 1.96,
                            "purchase_time": 1585708464,
                            "reference_id": 155032456528,
                            "shortcode": "DIGITEVEN_R_10_1.96_1585708464_1T",
                            "transaction_id": 155032464828,
                            "transaction_time": 1585708467
                          },
             */
            string contract_id = "";
            double sell_price = 0, buy_price = 0;
            for (int j = 0, i = 0; j < this._exe_bet_queue.Count; j++)
            {
                contract_id = this._exe_bet_queue.Peek().contract_id;
                int find = 0;
                foreach (JObject transaction in transactions)
                {
                    if (contract_id.Equals(transaction.Value<string>("contract_id"))&& transaction.Value<string>("action_type").Equals("sell"))
                    {
                        find = 1;
                        i = getIndexOfShortcode(transaction.Value<string>("shortcode"));
                        this._statement_last_row.Timestamp = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(double.Parse(transaction.Value<string>("purchase_time"))).ToString("yyyy-MM-dd HH:mm:ss");
                        this._statement_last_row.Reference = transaction.Value<string>("transaction_id");
                        this._statement_last_row.TradeType = transaction.Value<string>("shortcode").Split('_')[0].Replace("DIGIT", "");
                        this._statement_last_row.Market = this._Param_Markets[i];
                        this._statement_last_row.BuyPrice = $"{this._exe_bet_queue.Peek().buy_price}";
                        this._statement_last_row.SortColumn = transaction.Value<string>("purchase_time");
                        double.TryParse(transaction.Value<string>("payout"), out buy_price);
                        double.TryParse(transaction.Value<string>("amount"), out sell_price);
                        double.TryParse(transaction.Value<string>("balance_after"), out this._current_balance);
                        if (sell_price == 0)
                        { //loss
                            this._statement_last_row.ProfitLoss = $"-{this._exe_bet_queue.Peek().buy_price}";
                            this._exe_lost_Markets[i]++;
                            this._loss_count++;
                            this._loss_amount += this._exe_bet_queue.Peek().buy_price;
                            
                            this._exe_sequence_loss_count[i]++;
                            this._last_win_loss_status[i] = 1;
                            if(this._exe_mg_break_ticks[i]<0) this._exe_mg_break_ticks[i] = 0;
                            //this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},complete contract {this._statement_last_row.Market},loss {-buy_price},{this._statement_last_row.Timestamp},{this._statement_last_row.Reference},{transaction.Value<string>("shortcode")}\n";

                            if (!this.mode_silent_chk.Checked)
                            {
                                player.SoundLocation = $"loss.wav";//{System.Environment.CurrentDirectory}
                                player.Play();
                            }
                            
                        }
                        else
                        {
                            sell_price -= this._exe_bet_queue.Peek().buy_price;
                            this._statement_last_row.ProfitLoss = $"{sell_price}";
                            this._win_count++;
                            this._win_amount += sell_price;
                            this._exe_sequence_loss_count[i] = 0;
                            this._last_win_loss_status[i] = 0;
                            this._exe_mg_break_ticks[i] = -1;

                            //this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},complete contract {this._statement_last_row.Market},win {sell_price},{this._statement_last_row.Timestamp},{this._statement_last_row.Reference},{transaction.Value<string>("shortcode")}\n";
                            if (!this.mode_silent_chk.Checked)
                            {
                                player.SoundLocation = $"coin.wav";//{System.Environment.CurrentDirectory}
                                player.Play();
                            }
                        }

                        this._exe_bet_queue.Dequeue();
                        this.BuyAfterProcess_UI();
                        if (this._exe_proposal_queue.Count > 0&&this._stop_==0)
                        {
                            sendMessageAPI(this._exe_proposal_queue.Dequeue().Replace("_PAYAMOUNT_", this.getPayAmountInProposal(i).ToString()));
                            this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},statement proposal " ;
                        }
                        

                        break;
                    }
                }
                if (find == 0)
                {
                    //sendMessageAPI(this.search_profit_request);
                    //sendMessageAPI(this.search_transaction_request.Replace("_STARTDATE_", this._exe_bet_queue.Peek().transaction_time));
                    //this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},re {this._exe_bet_queue.Peek().contract_id} "+new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(double.Parse(this._exe_bet_queue.Peek().transaction_time)).ToString();
                }
                break;
            }
        }

        private string getOddEvenInProposal(int i)
        {
            int f = this._exe_lastdigit_Markets[i];
            if (this._Param_BuyReserve[i] == 1) f = 1 - f;
            return f==0?"EVEN":"ODD";
        }
        public void sendMessageAPI(string _jsonString)
        {
            //if(this._stop_>0)return;
            //MessageBox.Show("Sending message");
            if (is_connected)
            {
                BinaryNetSocket.Send(_jsonString);
                Debug.WriteLine("Message Send: " + _jsonString);
            }
        }

        private void token_txt_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            this.title_bar.Location = new Point(0,0);
            this.title_bar.Size = new Size(_w,40);
            this.title_lab.Location = new Point(_w/2-this.title_lab.Width/2, 12);
            this.min_btn.Location = new Point(_w-76, 1);
            this.min_btn.Size = new Size(38, 38);
            this.close_btn.Location = new Point(_w-38, 1);
            this.close_btn.Size = new Size(38, 38);
            this.min_btn.Image = global::OddEvenBotApp.Properties.Resources.min_inactive;
            this.close_btn.Image = global::OddEvenBotApp.Properties.Resources.close_inactive1;
            this.footer_bar.Size = new Size(_w, 60);
            this.footer_bar.Location = new Point(0, _h - this.footer_bar.Height);
            this.version_lab.Size = new Size(98, 25);
            this.version_lab.Location = new Point(_w- 300, 12);
            this.copyright_lab.Location = new Point(_w - 300, 35);
            this.status_account_lab.Location = new Point(20, 15);
            this.status_time_lab.Location = new Point(20, 35);
            this.status_winloss_lab.Location = new Point(195, 15);
            this.status_totalprofit_lab.Location = new Point(-195, -35);
            this.status_runs_lab.Location = new Point(195,35);// (325, 15);
            this.status_account_val_lab.Location = new Point(70, 15);
            this.status_time_val_lab.Location = new Point(70, 35);
            this.status_winloss_val_lab.Location = new Point(270, 15);
            this.status_totalprofit_val_lab.Location = new Point(-270, -35);
            this.status_runs_val_lab.Location = new Point(270,35);// (370, 15);
            this.info_start_pl.Location = new Point(20, 60);
            this.info_current_pl.Location = new Point(20+(this.info_start_pl.Width + 20), 60);
            this.info_target_pl.Location = new Point(20+(this.info_start_pl.Width + 20)*2, 60);
            this.info_stop_pl.Location = new Point(20+(this.info_start_pl.Width + 20)*2+ this.info_target_pl.Width+20, 60);
            this.logo_text_lab.Size = new Size(240, 100);
            this.logo_icon_lab.Size = new Size(120, 100);
            this.logo_text_lab.Location = new Point(_w-370, this.title_bar.Height+20);
            this.logo_icon_lab.Location = new Point(_w-130, this.title_bar.Height + 5);
            this.main_pl.Size = new Size(_w, _h - this.title_bar.Height - this.footer_bar.Height - 110);
            this.main_pl.Location = new Point(0,150);
            this.statement_grid_title_pl.Size = new Size(420, 26);
            this.statement_grid_title_pl.Location = new Point(_w - this.statement_grid_title_pl.Width, 1);
            this.statement_grid.Size = new Size(this.statement_grid_title_pl.Width, this.main_pl.Height - this.statement_grid_title_pl.Height);
            this.statement_grid.Location = new Point(_w - this.statement_grid_title_pl.Width, this.statement_grid_title_pl.Height);
            this.param_grid.Size = new Size(_w- this.statement_grid.Width, this.main_pl.Height - this.mode_pl.Height);
            this.param_grid.Location = new Point(0, 0);
            this.mode_pl.Location = new Point(_w - this.statement_grid.Width - this.mode_pl.Width, this.param_grid.Height);
            this.token_pl.Location = new Point(0, this.param_grid.Height-2);

            this.tick_show_pl.Top = 330;
            this.tick_show_pl.Left = 180;
        }

        private void min_btn_MouseEnter(object sender, EventArgs e)
        {
            this.min_btn.Image = global::OddEvenBotApp.Properties.Resources.min_active;
        }

        private void min_btn_MouseLeave(object sender, EventArgs e)
        {
            this.min_btn.Image = global::OddEvenBotApp.Properties.Resources.min_inactive;
        }

        private void min_btn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void close_btn_MouseEnter(object sender, EventArgs e)
        {
            this.close_btn.Image = global::OddEvenBotApp.Properties.Resources.close_active;
        }

        private void close_btn_MouseLeave(object sender, EventArgs e)
        {
            this.close_btn.Image = global::OddEvenBotApp.Properties.Resources.close_inactive1;
        }

        private void close_btn_Click(object sender, EventArgs e)
        {
            if(this._exe_proposal_queue.Count + this._exe_bet_queue.Count > 0){
                if(MessageBox.Show($"Sorry, can't exit! But do you want to cloase bot strongly?\n " + (this._exe_proposal_queue.Count > 0 ? this._exe_proposal_queue.Count.ToString() + " non-finished proposal(s)" : "")+"\n "+ (this._exe_bet_queue.Count > 0 ? this._exe_bet_queue.Count.ToString() + " non-finished statement(s)" : ""),"Confirm Exit", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Application.Exit();
                }
                this._stop_ = 1;
                if (this._stop_ > 0) this.stop_start_btn.Image = global::OddEvenBotApp.Properties.Resources.play_btn;
                else this.stop_start_btn.Image = global::OddEvenBotApp.Properties.Resources.stop_btn;
                return;
            }
            Application.Exit();
        }

        private long _second_clock_=0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            long v = ++_second_clock_;
            var h = Math.Floor(decimal.Parse((v / 3600).ToString()));
            var m = Math.Floor(decimal.Parse(((v % 3600) / 60).ToString()));
            var s = v % 60;
            this.status_time_val_lab.Text = (h < 10 ? "0" : "") + $"{h}"+ (m < 10 ? ":0" : ":") + $"{m}"+ (s < 10 ? ":0" : ":") + $"{s}";
        }
        public class ContractParam {
            public ContractParam(string contract_id, string shortcode, string transaction_id)
            {
                this.contract_id = contract_id;
                this.shortcode = shortcode;
                this.transaction_id = transaction_id;
            }
            public ContractParam(string contract_id, double buy_price,string transaction_time)
            {
                this.contract_id = contract_id;
                this.buy_price = buy_price;
                this.transaction_time = transaction_time;
            }
            public string contract_id { get; set; }
            public string transaction_time { get; set; }
            public double buy_price { get; set; }
            public string shortcode { get; set; }
            public string transaction_id { get; set; }
        }
        public class StateMentRow {
            public string Timestamp { get; set; }
            public string Reference { get; set; }
            public string TradeType { get; set; }
            public string Market { get; set; }
            public string BuyPrice { get; set; }
            public string ProfitLoss { get; set; }
            public string SortColumn { get; set; }
            private int IsLock { get; set; }
        }

        private void StartStopProcess() {
            
            if (this._stop_ > 0)
            {
                this.stop_start_btn.Image = global::OddEvenBotApp.Properties.Resources.play_btn;
                this.timer1.Enabled = false;
            }
            else
            {
                this.stop_start_btn.Image = global::OddEvenBotApp.Properties.Resources.stop_btn;
                this.timer1.Enabled = true;
            }
            this._alert_confirm = false;
        }
        private void stop_start_btn_Click(object sender, EventArgs e)
        {
            this._stop_ = 1 - this._stop_;
            StartStopProcess();
        }

        private void r10ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this._exe_tick_log_market[0]);
        }

        private void writeLogToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.IO.StreamWriter _buy_log_file_writer_1 = new System.IO.StreamWriter("buy_log.csv");
                _buy_log_file_writer_1.WriteLine(this._buy_log_file_body_);
                _buy_log_file_writer_1.Close();
            }
            catch (Exception) { }
        }

        private void param_grid_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private string _preview_cell_ = "";
        private string[] _params_keys_ = "Param_OnOff,Param_SequenceTrigger,Param_BuyReserve,Param_DefaultStake,Param_MGMultiplier,Param_NextStakeDueMG,Param_MGTriesBeforeBreak,Param_LossBreakeTicks".Split(',');
        private void param_grid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex > 7||(e.RowIndex==5&&e.ColumnIndex==1)) {
                e.Cancel = true;
                return;
            }
            if (e.RowIndex == 1 || e.RowIndex == 3 || e.RowIndex == 4 || e.RowIndex == 5 || e.RowIndex == 6 || e.RowIndex == 7) {
                this._preview_cell_ = this.param_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
            }
        }

        private void param_grid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
        }

        private void param_grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == 1 || e.RowIndex == 3 || e.RowIndex == 4 || e.RowIndex == 5 || e.RowIndex == 6 || e.RowIndex == 7)
            {
                double v = -1;
                double.TryParse(this.param_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), out v);
                if ((!this.param_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().Equals("0")) && v == 0)
                {
                    this.param_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = this._preview_cell_;
                    return;
                }

                int i = e.ColumnIndex - 2;
                if(e.RowIndex == 5 && e.ColumnIndex > 1) {
                    this.param_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.Teal;
                    this.param_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.Font = new Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    this._exe_mg_lock[i] = true;
                }
                
                if (i < 0) {
                    for (i = 0; i < this._market_cnt; i++) this.param_grid.Rows[e.RowIndex].Cells[i+2].Value = v;
                    i = -1;
                }
                string s = "";
                switch (e.RowIndex)
                {
                    //"Param_OnOff,Param_SequenceTrigger,Param_BuyReserve,Param_DefaultStake,Param_MGMultiplier,Param_NextStakeDueMG,Param_MGTriesBeforeBreak,Param_LossBreakeTicks".Split(',');
                    case 1:
                        if (i < 0) 
                            for (i = 0; i < this._market_cnt; i++) this._Param_SequenceTrigger[i] = int.Parse(v.ToString());
                        else this._Param_SequenceTrigger[i] = int.Parse(v.ToString());
                        foreach (var t in this._Param_SequenceTrigger) s += (s.Equals("")?"":",") + $"{t}";
                        break;
                    case 3:
                        if (i < 0)
                            for (i = 0; i < this._market_cnt; i++) this._Param_DefaultStake[i] = double.Parse(v.ToString());
                        else this._Param_DefaultStake[i] = v;
                        foreach (var t in this._Param_DefaultStake) s += (s.Equals("") ? "" : ",") + $"{t}";
                        break;
                    case 4:
                        if (i < 0)
                            for (i = 0; i < this._market_cnt; i++) this._Param_MGMultiplier[i] = double.Parse(v.ToString());
                        else this._Param_MGMultiplier[i] = v;
                        foreach (var t in this._Param_MGMultiplier) s += (s.Equals("") ? "" : ",") + $"{t}";
                        break;
                    case 5:
                        if (i < 0)
                        {
                            //for (i = 0; i < this._market_cnt; i++) this._Param_NextStakeDueMG[i] = int.Parse(v.ToString());
                        }
                        else this._Param_NextStakeDueMG[i] = double.Parse(v.ToString());
                        foreach (var t in this._Param_NextStakeDueMG) s += (s.Equals("") ? "" : ",") + $"{t}";
                        break;
                    case 6:
                        if (i < 0)
                            for (i = 0; i < this._market_cnt; i++) this._Param_MGTriesBeforeBreak[i] = int.Parse(v.ToString());
                        else this._Param_MGTriesBeforeBreak[i] = int.Parse(v.ToString());
                        foreach (var t in this._Param_MGTriesBeforeBreak) s += (s.Equals("") ? "" : ",") + $"{t}";
                        break;
                    case 7:
                        if (i < 0)
                            for (i = 0; i < this._market_cnt; i++) this._Param_LossBreakeTicks[i] = int.Parse(v.ToString());
                        else this._Param_LossBreakeTicks[i] = int.Parse(v.ToString());
                        foreach (var t in this._Param_LossBreakeTicks) s += (s.Equals("") ? "" : ",") + $"{t}";
                        break;
                    default:
                        break;
                }
                Configuration configuration = ConfigurationManager.
            OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);
                configuration.AppSettings.Settings[this._params_keys_[e.RowIndex]].Value = s;
                configuration.Save();
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        private void param_grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == 0 || e.RowIndex == 2)
            {
                double v = 1;
                double.TryParse(this.param_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), out v);
                if (this.param_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().ToLower().Equals("true")) v = 1;
                if (this.param_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString().ToLower().Equals("false")) v = 0;
                v = 1 - v;
                this.param_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = v;
                int i = e.ColumnIndex - 2;
                if (i < 0) {
                    for (i = 0; i < this._market_cnt; i++) this.param_grid.Rows[e.RowIndex].Cells[i + 2].Value = v;
                    i = -1;
                }
                string s = "";
                if (e.RowIndex == 0)
                {
                    if (i < 0)
                        for (i = 0; i < this._market_cnt; i++)
                        {
                            this._Param_OnOff[i] = int.Parse(v.ToString());
                            if (v == 0)
                            {
                                this.param_grid.Rows[_last_digit_row_index].Cells[i + 2].Value = "-";
                            }
                            else if (!this._exe_tick_started[i])
                            {
                                //sendMessageAPI(this.ticks_request.Replace("_MARKET_NAME_", this._Param_Markets[i]));
                                this._exe_tick_started[i] = true;
                            }
                        }
                    else { 
                        this._Param_OnOff[i] = int.Parse(v.ToString());
                        if (v == 0) this.param_grid.Rows[_last_digit_row_index].Cells[i + 2].Value = "-";
                        else if (!this._exe_tick_started[i]) { 
                            //sendMessageAPI(this.ticks_request.Replace("_MARKET_NAME_", this._Param_Markets[i]));
                            this._exe_tick_started[i] = true;
                        }
                    }

                    foreach (var t in this._Param_OnOff) s += (s.Equals("") ? "" : ",") + $"{t}";
                }
                else if (e.RowIndex == 2) {
                    if (i < 0)
                        for (i = 0; i < this._market_cnt; i++) this._Param_BuyReserve[i] = int.Parse(v.ToString());
                    else this._Param_BuyReserve[i] = int.Parse(v.ToString());
                    foreach (var t in this._Param_BuyReserve) s += (s.Equals("") ? "" : ",") + $"{t}";
                }
                Configuration configuration = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);
                configuration.AppSettings.Settings[this._params_keys_[e.RowIndex]].Value = s;
                configuration.Save();
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        private void title_lab_Click(object sender, EventArgs e)
        {
        }

        private void title_bar_DragEnter(object sender, DragEventArgs e)
        {
            MessageBox.Show("asd");
        }


        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        private void title_bar_MouseMove(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        private void MainForm_ParentChanged(object sender, EventArgs e)
        {
            
        }

        private void mode_silent_chk_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void mode_martingale_chk_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < this._market_cnt; i++)
            {
                this.param_grid.Rows[5].Cells[i + 2].Value = "0";
                this._exe_sequence_loss_count[i]=0;
            }
            if (this.mode_martingale_chk.Checked == true)
            {

            }
            else { 
            
            }
        }
    }
}
