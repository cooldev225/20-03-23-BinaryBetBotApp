using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OddEvenBotApp
{
    class Histories
    {
        /*
         private void BuyAfterProcess(double balance, double betprice, double getprice, string timestamp, string transid, string shortcode) {
            //MessageBox.Show($"{balance}, double {betprice}, double {getprice}, string {timestamp}, string {transid}, string {shortcode}");
            
            //"shortcode": "DIGITODD_R_50_0.96_1585364183_1T"
            int i = getIndexOfShortcode(shortcode);
            double val = this._current_balance - balance;
            this._statement_last_row.Timestamp = timestamp;
            this._statement_last_row.Reference = transid;
            this._statement_last_row.TradeType = shortcode.Split('_')[0].Replace("DIGIT", "");
            this._statement_last_row.Market = this._Param_Markets[i];
            this._statement_last_row.BuyPrice = getprice.ToString();

            if (val>0)
            { //loss
                this._statement_last_row.ProfitLoss = $"-{betprice}";
                this._exe_lost_Markets[i]++;
                this._loss_count++;
                this._loss_amount += betprice;
                //this is a moment of loss
                this._exe_sequence_loss_count[i]++;
                
                //
               // if (this._last_win_loss_status[i] == 1)
               // {
              //      this._exe_mg_level[i] = this._exe_sequence_Markets[i] - this._Param_SequenceTrigger[i];
               // }
               // else
                //    this._exe_mg_level[i] = 0;
                

                //if (this._exe_mg_level[i] == this._Param_MGTriesBeforeBreak[i]) this._exe_sequence_Markets[i] = 0;
                this._last_win_loss_status[i] = 1;
            }
            else {
                this._statement_last_row.ProfitLoss = $"{getprice - betprice}";
                this._win_count++;
                this._win_amount += getprice - betprice;
                if (this._last_win_loss_status[i] == 1) this._exe_sequence_loss_count [i] = 0;
    }

            this._current_balance = balance;

            this.BuyAfterProcess_UI();
        }
    private void SearchProfitAfterProcess(List<JObject> transactions)
    {
        
        string contract_id = "";
        double sell_price = 0, balance = 0, buy_price = 0;
        for (int j = 0, i = 0; j < this._exe_bet_queue.Count; j++)
        {
            contract_id = this._exe_bet_queue.Peek().contract_id;
            int find = 0;
            foreach (JObject transaction in transactions)
            {
                if (contract_id.Equals(transaction.Value<string>("contract_id")))
                {
                    find = 1;
                    i = getIndexOfShortcode(transaction.Value<string>("shortcode"));
                    this._statement_last_row.Timestamp = transaction.Value<string>("purchase_time");
                    this._statement_last_row.Reference = transaction.Value<string>("transaction_id");
                    this._statement_last_row.TradeType = transaction.Value<string>("shortcode").Split('_')[0].Replace("DIGIT", "");
                    this._statement_last_row.Market = this._Param_Markets[i];
                    this._statement_last_row.BuyPrice = transaction.Value<string>("buy_price");
                    double.TryParse(transaction.Value<string>("buy_price"), out buy_price);
                    double.TryParse(transaction.Value<string>("sell_price"), out sell_price);
                    double.TryParse(transaction.Value<string>("sell_price"), out balance);
                    if (sell_price == 0)
                    { //loss
                        this._statement_last_row.ProfitLoss = $"-{buy_price}";
                        this._exe_lost_Markets[i]++;
                        this._loss_count++;
                        this._loss_amount += buy_price;
                        
                        //if (this._last_win_loss_status[i] == 1)
                       // {
                       //     this._exe_mg_level[i] = this._exe_sequence_Markets[i] - this._Param_SequenceTrigger[i];
                       // }
                       // else
                       ///     this._exe_mg_level[i] = 0;
                        
                        this._exe_sequence_loss_count[i]++;
                        this._last_win_loss_status[i] = 1;
                        this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},complete contract {this._statement_last_row.Market},loss {-buy_price},{this._statement_last_row.Timestamp},{this._statement_last_row.Reference},{transaction.Value<string>("shortcode")}\n";
                    }
                    else
                    {
                        sell_price -= buy_price;
                        this._statement_last_row.ProfitLoss = $"{sell_price}";
                        this._win_count++;
                        this._win_amount += sell_price;
                        this._exe_sequence_loss_count[i] = 0;
                        this._last_win_loss_status[i] = 0;
                        this._buy_log_file_body_ += $"{DateTime.Now.ToString("HH:mm:ss")},complete contract {this._statement_last_row.Market},win {sell_price},{this._statement_last_row.Timestamp},{this._statement_last_row.Reference},{transaction.Value<string>("shortcode")}\n";
                    }

                    //this._exe_bet_queue.Dequeue();
                    this._exe_bet_queue.Dequeue();
                    this.BuyAfterProcess_UI();
                    break;
                }
            }
            if (find == 0)
            {
                sendMessage(this.search_profit_request);
            }
            break;
        }
    }
    //void SearchProfitAfterProcess
        private void DelayTaskStamp(double second) {
            CancellationTokenSource source = new CancellationTokenSource();
            var t = Task.Run(async delegate
            {
                await Task.Delay(TimeSpan.FromSeconds(second), source.Token);
                return 42;
            });
            source.Cancel();
            try
            {
                t.Wait();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                    Console.WriteLine("{0}: {1}", e.GetType().Name, e.Message);
            }
            Console.Write("Task t Status: {0}", t.Status);
            if (t.Status == TaskStatus.RanToCompletion)
                Console.Write(", Result: {0}", t.Result);
            source.Dispose();
        }
        private void SendingBuyRequest(int i,string market) {
            if (this._exe_bet_queue.Count > 1) {
                DelayTaskStamp(1);
                SendingBuyRequest(i,market);
            } 
            
        }

         */
    }
}
