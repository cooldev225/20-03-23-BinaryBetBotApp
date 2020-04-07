
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OddEvenBotApp.Model
{
    public class AuthorizeModel
    {
        public Authorize authorize { get; set; }
        public string msg_type { get; set; }
        public class Authorize {
            public Account_list[] account_list { get; set; }
            public string balance { get; set; }
            public string country { get; set; }
            public string currency { get; set; }
            public string email { get; set; }
            public string fullname { get; set; }
            public int is_virtual { get; set; }
            public string landing_company_fullname { get; set; }
            public string landing_company_name { get; set; }
            public Local_currencies local_currencies { get; set; }
            public string loginid { get; set; }
            public string[] scopes { get; set; }
            public string[] upgradeable_landing_companies { get; set; }
            public class Account_list
            {
                public string currency { get; set; }
                public int is_disabled { get; set; }
                public int is_virtual { get; set; }
                public string landing_company_name { get; set; }
                public string loginid { get; set; }
            }
            public class Local_currencies
            {
                public ROL ROL { get; set; }
            }
            public class ROL
            {
                public int fractional_digits { get; set; }
            }
        }
    }
}
