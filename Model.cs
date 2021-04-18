using System;
using System.Collections.Generic;
using System.Text;

namespace SMEDKRecorder
{
    class Model
    {
        public string ftp_host { get; set; }
        public string ftp_login { get; set; }
        public string ftp_password { get; set; }
        public string local_time { get; set; }
        public string transform_view_host { get; set; }
        public string transform_view_port { get; set; }
    }
}
