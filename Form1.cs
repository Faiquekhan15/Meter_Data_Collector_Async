﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Data.Odbc;
using System.Timers;
using System.IO.Ports;
using System.Diagnostics;
using MDC_Server.Storage_Structures;

namespace MDC_Server
{
    public partial class Form1 : Form
    {
        private TcpListener server;
        private DateTime DT;
        private bool autoAlarmEventRead = true;
        private System.Timers.Timer ConnectionCheck;
        private System.Timers.Timer mode2Initiate;
        private System.Timers.Timer LoadProfileReadTime;
        private Dictionary<UInt32, NetworkStream> Stream_Object_Dict = new Dictionary<UInt32, NetworkStream>();

        private string myconn = "DRIVER={MySQL ODBC 3.51 Driver};Database=meter;Server=localhost;Port=2142;UID=root;PWD=TRANSFOPOWER@123@321;";
        public Form1()
        {
            InitializeComponent();
            SetConStatTimer();
            setMode2Initiate();
            setLoadProfileReadingTimer();

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    IPBox.Items.Add(ip.ToString());
                }
            }
        }
        private void setLoadProfileReadingTimer()
        {
            LoadProfileReadTime = new System.Timers.Timer(10000);
            LoadProfileReadTime.AutoReset = true;
            LoadProfileReadTime.Enabled = true;
        }
        private void setMode2Initiate()
        {
            mode2Initiate = new System.Timers.Timer(10000);
            mode2Initiate.Elapsed += mode2Initiate_Elapsed;
            mode2Initiate.AutoReset = true;
            mode2Initiate.Enabled = true;
        }
        private void SetConStatTimer()
        {
            ConnectionCheck = new System.Timers.Timer(600000);
            ConnectionCheck.Elapsed += ConnectionCheck_Elapsed;
            ConnectionCheck.AutoReset = true;
            ConnectionCheck.Enabled = true;
        }
        protected bool IsDigitsOnly(string str)
        {
            if (str.Length < 1)
            {
                return false;
            }
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }
            return true;
        }
        private string ParseInt(string intstr)
        {
            if (IsDigitsOnly(intstr))
            {
                return intstr;
            }
            return "65535";
        }
        private string ConvertToX4ByteString(int value)
        {
            string hexString = "";
            string temp = value.ToString("X4");
            hexString = temp[0] + "" + temp[1] + " " + temp[2] + "" + temp[3];
            return hexString;
        }
        private string ConvertToByteString(byte[] bytes, int length)
        {
            string hexString = "";
            for (int i = 0; i < length; i++)
            {
                hexString += bytes[i].ToString("X2") + " ";
            }
            return hexString.Substring(0, hexString.Length - 1);
        }
        private void ExecuteNonQurey(string query)
        {
            DBGetSet db = new DBGetSet();
            db.Query = query;
            db.ExecuteNonQuery();
        }
        private DataTable ExecuteReader(string query)
        {
            DBGetSet db = new DBGetSet();
            db.Query = query;
            return db.ExecuteReader();
        }
       
        private int read_billing_data(NetworkStream stream, byte[] data, uint MSN, string global_device_id)
        {
            byte[] current_MonthBilling = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 7, 1, 0, 98, 1, 0, 255, 3, 0 };
            byte[] blk_1 = { 0, 1, 0, 48, 0, 1, 0, 7, 192, 2, 129, 0, 0, 0, 1 };
            byte[] blk_2 = { 0, 1, 0, 48, 0, 1, 0, 7, 192, 2, 129, 0, 0, 0, 2 };

            List<byte> res1 = new List<byte>();
            List<byte> res2 = new List<byte>();
            List<byte> res3 = new List<byte>();
            int count = 0;
            stream.Write(current_MonthBilling, 0, current_MonthBilling.Length);
            count = stream.Read(data, 0, data.Length);
            if (data[8] == 14)
            {
                //Debugdata(data, count);
                throw new Exception("data Error in current bill read.");
                //return 1;
            }
            if (count < 274)
            {
                return 0;
            }
            for (int i = 0; i < count; i++)
            {
                res1.Add(data[i]);
            }

            UInt16 year_b1 = (UInt16)((res1[count - 274] << 8) | (res1[count - 273] << 0));
            string date_time_b1 = year_b1 + "/" + res1[count - 272] + "/" + res1[count - 271] + " " + res1[count - 269] + ":" + res1[count - 268] + ":" + res1[count - 267];

            double active_energy_pos_t1 = ((UInt32)((res1[count - 261] << 24) | (res1[count - 260] << 16) | (res1[count - 259] << 8) | (res1[count - 258] << 0))) * 0.001;
            double active_energy_pos_t2 = ((UInt32)((res1[count - 256] << 24) | (res1[count - 255] << 16) | (res1[count - 254] << 8) | (res1[count - 253] << 0))) * 0.001;
            double active_energy_pos_t3 = ((UInt32)((res1[count - 251] << 24) | (res1[count - 250] << 16) | (res1[count - 249] << 8) | (res1[count - 248] << 0))) * 0.001;
            double active_energy_pos_t4 = ((UInt32)((res1[count - 246] << 24) | (res1[count - 245] << 16) | (res1[count - 244] << 8) | (res1[count - 243] << 0))) * 0.001;
            double active_energy_pos_tl = ((UInt32)((res1[count - 241] << 24) | (res1[count - 240] << 16) | (res1[count - 239] << 8) | (res1[count - 238] << 0))) * 0.001;

            /////////////////
            double active_energy_neg_t1 = ((UInt32)((res1[count - 236] << 24) | (res1[count - 235] << 16) | (res1[count - 234] << 8) | (res1[count - 233] << 0))) * 0.001;
            double active_energy_neg_t2 = ((UInt32)((res1[count - 231] << 24) | (res1[count - 230] << 16) | (res1[count - 229] << 8) | (res1[count - 228] << 0))) * 0.001;
            double active_energy_neg_t3 = ((UInt32)((res1[count - 226] << 24) | (res1[count - 225] << 16) | (res1[count - 224] << 8) | (res1[count - 223] << 0))) * 0.001;
            double active_energy_neg_t4 = ((UInt32)((res1[count - 221] << 24) | (res1[count - 220] << 16) | (res1[count - 219] << 8) | (res1[count - 218] << 0))) * 0.001;
            double active_energy_neg_tl = ((UInt32)((res1[count - 216] << 24) | (res1[count - 215] << 16) | (res1[count - 214] << 8) | (res1[count - 213] << 0))) * 0.001;

            /////////////////
            double active_energy_abs_t1 = ((UInt32)((res1[count - 211] << 24) | (res1[count - 210] << 16) | (res1[count - 209] << 8) | (res1[count - 208] << 0))) * 0.001;
            double active_energy_abs_t2 = ((UInt32)((res1[count - 206] << 24) | (res1[count - 205] << 16) | (res1[count - 204] << 8) | (res1[count - 203] << 0))) * 0.001;
            double active_energy_abs_t3 = ((UInt32)((res1[count - 201] << 24) | (res1[count - 200] << 16) | (res1[count - 199] << 8) | (res1[count - 198] << 0))) * 0.001;
            double active_energy_abs_t4 = ((UInt32)((res1[count - 196] << 24) | (res1[count - 195] << 16) | (res1[count - 194] << 8) | (res1[count - 193] << 0))) * 0.001;
            double active_energy_abs_tl = ((UInt32)((res1[count - 191] << 24) | (res1[count - 190] << 16) | (res1[count - 189] << 8) | (res1[count - 188] << 0))) * 0.001;
            /////////////////
            double reactive_energy_pos_t1 = ((UInt32)((res1[count - 186] << 24) | (res1[count - 185] << 16) | (res1[count - 184] << 8) | (res1[count - 183] << 0))) * 0.001;
            double reactive_energy_pos_t2 = ((UInt32)((res1[count - 181] << 24) | (res1[count - 180] << 16) | (res1[count - 179] << 8) | (res1[count - 178] << 0))) * 0.001;
            double reactive_energy_pos_t3 = ((UInt32)((res1[count - 176] << 24) | (res1[count - 175] << 16) | (res1[count - 174] << 8) | (res1[count - 173] << 0))) * 0.001;
            double reactive_energy_pos_t4 = ((UInt32)((res1[count - 171] << 24) | (res1[count - 170] << 16) | (res1[count - 169] << 8) | (res1[count - 168] << 0))) * 0.001;
            double reactive_energy_pos_tl = ((UInt32)((res1[count - 166] << 24) | (res1[count - 165] << 16) | (res1[count - 164] << 8) | (res1[count - 163] << 0))) * 0.001;

            double reactive_energy_neg_t1 = ((UInt32)((res1[count - 161] << 24) | (res1[count - 160] << 16) | (res1[count - 159] << 8) | (res1[count - 158] << 0))) * 0.001;
            double reactive_energy_neg_t2 = ((UInt32)((res1[count - 156] << 24) | (res1[count - 155] << 16) | (res1[count - 154] << 8) | (res1[count - 153] << 0))) * 0.001;
            double reactive_energy_neg_t3 = ((UInt32)((res1[count - 151] << 24) | (res1[count - 150] << 16) | (res1[count - 149] << 8) | (res1[count - 148] << 0))) * 0.001;
            double reactive_energy_neg_t4 = ((UInt32)((res1[count - 146] << 24) | (res1[count - 145] << 16) | (res1[count - 144] << 8) | (res1[count - 143] << 0))) * 0.001;
            double reactive_energy_neg_tl = ((UInt32)((res1[count - 141] << 24) | (res1[count - 140] << 16) | (res1[count - 139] << 8) | (res1[count - 138] << 0))) * 0.001;

            double reactive_energy_abs_t1 = ((UInt32)((res1[count - 136] << 24) | (res1[count - 135] << 16) | (res1[count - 134] << 8) | (res1[count - 133] << 0))) * 0.001;
            double reactive_energy_abs_t2 = ((UInt32)((res1[count - 131] << 24) | (res1[count - 130] << 16) | (res1[count - 129] << 8) | (res1[count - 128] << 0))) * 0.001;
            double reactive_energy_abs_t3 = ((UInt32)((res1[count - 126] << 24) | (res1[count - 125] << 16) | (res1[count - 124] << 8) | (res1[count - 123] << 0))) * 0.001;
            double reactive_energy_abs_t4 = ((UInt32)((res1[count - 121] << 24) | (res1[count - 120] << 16) | (res1[count - 119] << 8) | (res1[count - 118] << 0))) * 0.001;
            double reactive_energy_abs_tl = ((UInt32)((res1[count - 116] << 24) | (res1[count - 115] << 16) | (res1[count - 114] << 8) | (res1[count - 113] << 0))) * 0.001;



            double active_mdi_pos_t1 = ((UInt32)((res1[count - 86] << 24) | (res1[count - 85] << 16) | (res1[count - 84] << 8) | (res1[count - 83] << 0))) * 0.001;
            double active_mdi_pos_t2 = ((UInt32)((res1[count - 67] << 24) | (res1[count - 66] << 16) | (res1[count - 65] << 8) | (res1[count - 64] << 0))) * 0.001;
            double active_mdi_pos_t3 = ((UInt32)((res1[count - 48] << 24) | (res1[count - 47] << 16) | (res1[count - 46] << 8) | (res1[count - 45] << 0))) * 0.001;
            double active_mdi_pos_t4 = ((UInt32)((res1[count - 29] << 24) | (res1[count - 28] << 16) | (res1[count - 27] << 8) | (res1[count - 26] << 0))) * 0.001;
            double active_mdi_pos_tl = ((UInt32)((res1[count - 10] << 24) | (res1[count - 9] << 16) | (res1[count - 8] << 8) | (res1[count - 7] << 0))) * 0.001;


            stream.Write(blk_1, 0, blk_1.Length);
            count = stream.Read(data, 0, data.Length);
            for (int i = 0; i < count; i++)
            {
                res2.Add(data[i]);
            }

            double active_mdi_neg_t1 = ((UInt32)((res2[count - 271] << 24) | (res2[count - 270] << 16) | (res2[count - 269] << 8) | (res2[count - 268] << 0))) * 0.001;
            double active_mdi_neg_t2 = ((UInt32)((res2[count - 252] << 24) | (res2[count - 251] << 16) | (res2[count - 250] << 8) | (res2[count - 249] << 0))) * 0.001;
            double active_mdi_neg_t3 = ((UInt32)((res2[count - 233] << 24) | (res2[count - 232] << 16) | (res2[count - 231] << 8) | (res2[count - 230] << 0))) * 0.001;
            double active_mdi_neg_t4 = ((UInt32)((res2[count - 214] << 24) | (res2[count - 213] << 16) | (res2[count - 212] << 8) | (res2[count - 211] << 0))) * 0.001;
            double active_mdi_neg_tl = ((UInt32)((res2[count - 195] << 24) | (res2[count - 194] << 16) | (res2[count - 193] << 8) | (res2[count - 192] << 0))) * 0.001;


            double active_mdi_abs_t1 = ((UInt32)((res2[count - 176] << 24) | (res2[count - 175] << 16) | (res2[count - 174] << 8) | (res2[count - 173] << 0))) * 0.001;
            double active_mdi_abs_t2 = ((UInt32)((res2[count - 157] << 24) | (res2[count - 156] << 16) | (res2[count - 155] << 8) | (res2[count - 154] << 0))) * 0.001;
            double active_mdi_abs_t3 = ((UInt32)((res2[count - 138] << 24) | (res2[count - 137] << 16) | (res2[count - 136] << 8) | (res2[count - 135] << 0))) * 0.001;
            double active_mdi_abs_t4 = ((UInt32)((res2[count - 119] << 24) | (res2[count - 118] << 16) | (res2[count - 117] << 8) | (res2[count - 116] << 0))) * 0.001;
            double active_mdi_abs_tl = ((UInt32)((res2[count - 100] << 24) | (res2[count - 99] << 16) | (res2[count - 98] << 8) | (res2[count - 97] << 0))) * 0.001;



            double cumulative_mdi_pos_t1 = ((UInt32)((data[count - 81] << 24) | (data[count - 80] << 16) | (data[count - 79] << 8) | (data[count - 78] << 0))) * 0.001;
            double cumulative_mdi_pos_t2 = ((UInt32)((data[count - 76] << 24) | (data[count - 75] << 16) | (data[count - 74] << 8) | (data[count - 73] << 0))) * 0.001;
            double cumulative_mdi_pos_t3 = ((UInt32)((data[count - 71] << 24) | (data[count - 70] << 16) | (data[count - 69] << 8) | (data[count - 68] << 0))) * 0.001;
            double cumulative_mdi_pos_t4 = ((UInt32)((data[count - 66] << 24) | (data[count - 65] << 16) | (data[count - 64] << 8) | (data[count - 63] << 0))) * 0.001;
            double cumulative_mdi_pos_tl = ((UInt32)((data[count - 61] << 24) | (data[count - 60] << 16) | (data[count - 59] << 8) | (data[count - 58] << 0))) * 0.001;




            double cumulative_mdi_neg_t1 = ((UInt32)((data[count - 56] << 24) | (data[count - 55] << 16) | (data[count - 54] << 8) | (data[count - 53] << 0))) * 0.001;
            double cumulative_mdi_neg_t2 = ((UInt32)((data[count - 51] << 24) | (data[count - 50] << 16) | (data[count - 49] << 8) | (data[count - 48] << 0))) * 0.001;
            double cumulative_mdi_neg_t3 = ((UInt32)((data[count - 46] << 24) | (data[count - 45] << 16) | (data[count - 44] << 8) | (data[count - 43] << 0))) * 0.001;
            double cumulative_mdi_neg_t4 = ((UInt32)((data[count - 41] << 24) | (data[count - 40] << 16) | (data[count - 39] << 8) | (data[count - 38] << 0))) * 0.001;
            double cumulative_mdi_neg_tl = ((UInt32)((data[count - 36] << 24) | (data[count - 35] << 16) | (data[count - 34] << 8) | (data[count - 33] << 0))) * 0.001;



            double cumulative_mdi_abs_t1 = ((UInt32)((data[count - 31] << 24) | (data[count - 30] << 16) | (data[count - 29] << 8) | (data[count - 28] << 0))) * 0.001;
            double cumulative_mdi_abs_t2 = ((UInt32)((data[count - 26] << 24) | (data[count - 25] << 16) | (data[count - 24] << 8) | (data[count - 23] << 0))) * 0.001;
            double cumulative_mdi_abs_t3 = ((UInt32)((data[count - 21] << 24) | (data[count - 20] << 16) | (data[count - 19] << 8) | (data[count - 18] << 0))) * 0.001;
            double cumulative_mdi_abs_t4 = ((UInt32)((data[count - 16] << 24) | (data[count - 15] << 16) | (data[count - 14] << 8) | (data[count - 13] << 0))) * 0.001;
            double cumulative_mdi_abs_tl = ((UInt32)((data[count - 11] << 24) | (data[count - 10] << 16) | (data[count - 9] << 8) | (data[count - 8] << 0))) * 0.001;



            double reset_count = (UInt32)((data[count - 6] << 24) | (data[count - 5] << 16) | (data[count - 4] << 8) | (data[count - 3] << 0));
            stream.Write(blk_2, 0, blk_2.Length);
            count = stream.Read(data, 0, data.Length);
            for (int i = 0; i < count; i++)
            {
                res3.Add(data[i]);
            }

            UInt16 mdi_reset_y = (UInt16)((res3[count - 12] << 8) | (res3[count - 11] << 0));
            string mdi_reset_datetime = mdi_reset_y + "/" + res3[count - 10] + "/" + res3[count - 9] + " " + res3[count - 7] + ":" + res3[count - 6] + ":" + res3[count - 5];


            ExecuteNonQurey("INSERT IGNORE INTO meter.billing_data_temp VALUES('" + MSN + "', '" + global_device_id + "', '" + date_time_b1 + "', '" + active_energy_pos_t1 + "', '" + active_energy_pos_t2 + "', '" + active_energy_pos_t3 + "', '" + active_energy_pos_t4 + "', '" + active_energy_pos_tl + "', '" + active_energy_neg_t1 + "', '" + active_energy_neg_t2 + "', '" + active_energy_neg_t3 + "', '" + active_energy_neg_t4 + "', '" + active_energy_neg_tl + "', '" + reactive_energy_pos_t1 + "', '" + reactive_energy_pos_t2 + "', '" + reactive_energy_pos_t3 + "', '" + reactive_energy_pos_t4 + "', '" + reactive_energy_pos_tl + "', '" + reactive_energy_neg_t1 + "', '" + reactive_energy_neg_t2 + "', '" + reactive_energy_neg_t3 + "', '" + reactive_energy_neg_t4 + "', '" + reactive_energy_neg_tl + "', '" + active_mdi_pos_t1 + "', '" + active_mdi_pos_t2 + "', '" + active_mdi_pos_t3 + "', '" + active_mdi_pos_t4 + "', '" + active_mdi_pos_tl + "', '" + active_mdi_neg_t1 + "', '" + active_mdi_neg_t2 + "', '" + active_mdi_neg_t3 + "', '" + active_mdi_neg_t4 + "', '" + active_mdi_neg_tl + "', '" + active_mdi_abs_t1 + "', '" + active_mdi_abs_t2 + "', '" + active_mdi_abs_t3 + "', '" + active_mdi_abs_t4 + "', '" + active_mdi_abs_tl + "', '" + cumulative_mdi_pos_t1 + "', '" + cumulative_mdi_pos_t2 + "', '" + cumulative_mdi_pos_t3 + "', '" + cumulative_mdi_pos_t4 + "', '" + cumulative_mdi_pos_tl + "', '" + cumulative_mdi_neg_t1 + "', '" + cumulative_mdi_neg_t2 + "', '" + cumulative_mdi_neg_t3 + "', '" + cumulative_mdi_neg_t4 + "', '" + cumulative_mdi_neg_tl + "', '" + cumulative_mdi_abs_t1 + "', '" + cumulative_mdi_abs_t2 + "', '" + cumulative_mdi_abs_t3 + "', '" + cumulative_mdi_abs_t4 + "', '" + cumulative_mdi_abs_tl + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");

            ExecuteNonQurey("UPDATE meter.meter SET job = 0 WHERE global_device_id = '" + global_device_id + "';");
            return 0;
        }
        //CHECKED
        private int read_monthly_billing_data(NetworkStream stream, byte[] data, uint MSN, string global_device_id)
        {
            byte[] last_MonthBilling = { 0, 1, 0, 48, 0, 1, 0, 32, 192, 1, 129, 0, 7, 1, 0, 98, 1, 0, 255, 2, 1, 2, 2, 4, 6, 0, 0, 0, 1, 6, 0, 0, 0, 1, 18, 0, 1, 18, 0, 0 };
            byte[] blk_1 = { 0, 1, 0, 48, 0, 1, 0, 7, 192, 2, 129, 0, 0, 0, 1 };
            byte[] blk_2 = { 0, 1, 0, 48, 0, 1, 0, 7, 192, 2, 129, 0, 0, 0, 2 };
            List<byte> res1 = new List<byte>();
            List<byte> res2 = new List<byte>();
            List<byte> res3 = new List<byte>();

            stream.Write(last_MonthBilling, 0, last_MonthBilling.Length);
            int count = stream.Read(data, 0, data.Length);
            if (data[8] == 14)
            {
                //DebugData(data, count);
                return 1;
                //throw new Exception("Data Error in monthly bill read.");
            }
            if (count < 274) { return 0; }
            for (int i = 0; i < count; i++)
            {
                res1.Add(data[i]);
            }

            UInt16 year_b1 = (UInt16)((res1[count - 274] << 8) | (res1[count - 273] << 0));
            string date_time_b1 = year_b1 + "/" + res1[count - 272] + "/" + res1[count - 271] + " " + res1[count - 269] + ":" + res1[count - 268] + ":" + res1[count - 267];

            double active_energy_pos_t1 = ((UInt32)((res1[count - 261] << 24) | (res1[count - 260] << 16) | (res1[count - 259] << 8) | (res1[count - 258] << 0))) * 0.001;
            double active_energy_pos_t2 = ((UInt32)((res1[count - 256] << 24) | (res1[count - 255] << 16) | (res1[count - 254] << 8) | (res1[count - 253] << 0))) * 0.001;
            double active_energy_pos_t3 = ((UInt32)((res1[count - 251] << 24) | (res1[count - 250] << 16) | (res1[count - 249] << 8) | (res1[count - 248] << 0))) * 0.001;
            double active_energy_pos_t4 = ((UInt32)((res1[count - 246] << 24) | (res1[count - 245] << 16) | (res1[count - 244] << 8) | (res1[count - 243] << 0))) * 0.001;
            double active_energy_pos_tl = ((UInt32)((res1[count - 241] << 24) | (res1[count - 240] << 16) | (res1[count - 239] << 8) | (res1[count - 238] << 0))) * 0.001;

            double active_energy_neg_t1 = ((UInt32)((res1[count - 236] << 24) | (res1[count - 235] << 16) | (res1[count - 234] << 8) | (res1[count - 233] << 0))) * 0.001;
            double active_energy_neg_t2 = ((UInt32)((res1[count - 231] << 24) | (res1[count - 230] << 16) | (res1[count - 229] << 8) | (res1[count - 228] << 0))) * 0.001;
            double active_energy_neg_t3 = ((UInt32)((res1[count - 226] << 24) | (res1[count - 225] << 16) | (res1[count - 224] << 8) | (res1[count - 223] << 0))) * 0.001;
            double active_energy_neg_t4 = ((UInt32)((res1[count - 221] << 24) | (res1[count - 220] << 16) | (res1[count - 219] << 8) | (res1[count - 218] << 0))) * 0.001;
            double active_energy_neg_tl = ((UInt32)((res1[count - 216] << 24) | (res1[count - 215] << 16) | (res1[count - 214] << 8) | (res1[count - 213] << 0))) * 0.001;

            double active_energy_abs_t1 = ((UInt32)((res1[count - 211] << 24) | (res1[count - 210] << 16) | (res1[count - 209] << 8) | (res1[count - 208] << 0))) * 0.001;
            double active_energy_abs_t2 = ((UInt32)((res1[count - 206] << 24) | (res1[count - 205] << 16) | (res1[count - 204] << 8) | (res1[count - 203] << 0))) * 0.001;
            double active_energy_abs_t3 = ((UInt32)((res1[count - 201] << 24) | (res1[count - 200] << 16) | (res1[count - 199] << 8) | (res1[count - 198] << 0))) * 0.001;
            double active_energy_abs_t4 = ((UInt32)((res1[count - 196] << 24) | (res1[count - 195] << 16) | (res1[count - 194] << 8) | (res1[count - 193] << 0))) * 0.001;
            double active_energy_abs_tl = ((UInt32)((res1[count - 191] << 24) | (res1[count - 190] << 16) | (res1[count - 189] << 8) | (res1[count - 188] << 0))) * 0.001;

            double reactive_energy_pos_t1 = ((UInt32)((res1[count - 186] << 24) | (res1[count - 185] << 16) | (res1[count - 184] << 8) | (res1[count - 183] << 0))) * 0.001;
            double reactive_energy_pos_t2 = ((UInt32)((res1[count - 181] << 24) | (res1[count - 180] << 16) | (res1[count - 179] << 8) | (res1[count - 178] << 0))) * 0.001;
            double reactive_energy_pos_t3 = ((UInt32)((res1[count - 176] << 24) | (res1[count - 175] << 16) | (res1[count - 174] << 8) | (res1[count - 173] << 0))) * 0.001;
            double reactive_energy_pos_t4 = ((UInt32)((res1[count - 171] << 24) | (res1[count - 170] << 16) | (res1[count - 169] << 8) | (res1[count - 168] << 0))) * 0.001;
            double reactive_energy_pos_tl = ((UInt32)((res1[count - 166] << 24) | (res1[count - 165] << 16) | (res1[count - 164] << 8) | (res1[count - 163] << 0))) * 0.001;

            double reactive_energy_neg_t1 = ((UInt32)((res1[count - 161] << 24) | (res1[count - 160] << 16) | (res1[count - 159] << 8) | (res1[count - 158] << 0))) * 0.001;
            double reactive_energy_neg_t2 = ((UInt32)((res1[count - 156] << 24) | (res1[count - 155] << 16) | (res1[count - 154] << 8) | (res1[count - 153] << 0))) * 0.001;
            double reactive_energy_neg_t3 = ((UInt32)((res1[count - 151] << 24) | (res1[count - 150] << 16) | (res1[count - 149] << 8) | (res1[count - 148] << 0))) * 0.001;
            double reactive_energy_neg_t4 = ((UInt32)((res1[count - 146] << 24) | (res1[count - 145] << 16) | (res1[count - 144] << 8) | (res1[count - 143] << 0))) * 0.001;
            double reactive_energy_neg_tl = ((UInt32)((res1[count - 141] << 24) | (res1[count - 140] << 16) | (res1[count - 139] << 8) | (res1[count - 138] << 0))) * 0.001;

            double reactive_energy_abs_t1 = ((UInt32)((res1[count - 136] << 24) | (res1[count - 135] << 16) | (res1[count - 134] << 8) | (res1[count - 133] << 0))) * 0.001;
            double reactive_energy_abs_t2 = ((UInt32)((res1[count - 131] << 24) | (res1[count - 130] << 16) | (res1[count - 129] << 8) | (res1[count - 128] << 0))) * 0.001;
            double reactive_energy_abs_t3 = ((UInt32)((res1[count - 126] << 24) | (res1[count - 125] << 16) | (res1[count - 124] << 8) | (res1[count - 123] << 0))) * 0.001;
            double reactive_energy_abs_t4 = ((UInt32)((res1[count - 121] << 24) | (res1[count - 120] << 16) | (res1[count - 119] << 8) | (res1[count - 118] << 0))) * 0.001;
            double reactive_energy_abs_tl = ((UInt32)((res1[count - 116] << 24) | (res1[count - 115] << 16) | (res1[count - 114] << 8) | (res1[count - 113] << 0))) * 0.001;

            double active_mdi_pos_t1 = ((UInt32)((res1[count - 86] << 24) | (res1[count - 85] << 16) | (res1[count - 84] << 8) | (res1[count - 83] << 0))) * 0.001;
            double active_mdi_pos_t2 = ((UInt32)((res1[count - 67] << 24) | (res1[count - 66] << 16) | (res1[count - 65] << 8) | (res1[count - 64] << 0))) * 0.001;
            double active_mdi_pos_t3 = ((UInt32)((res1[count - 48] << 24) | (res1[count - 47] << 16) | (res1[count - 46] << 8) | (res1[count - 45] << 0))) * 0.001;
            double active_mdi_pos_t4 = ((UInt32)((res1[count - 29] << 24) | (res1[count - 28] << 16) | (res1[count - 27] << 8) | (res1[count - 26] << 0))) * 0.001;
            double active_mdi_pos_tl = ((UInt32)((res1[count - 10] << 24) | (res1[count - 9] << 16) | (res1[count - 8] << 8) | (res1[count - 7] << 0))) * 0.001;

            stream.Write(blk_1, 0, blk_1.Length);
            count = stream.Read(data, 0, data.Length);
            for (int i = 0; i < count; i++)
            {
                res2.Add(data[i]);
            }

            double active_mdi_neg_t1 = ((UInt32)((res2[count - 271] << 24) | (res2[count - 270] << 16) | (res2[count - 269] << 8) | (res2[count - 268] << 0))) * 0.001;
            double active_mdi_neg_t2 = ((UInt32)((res2[count - 252] << 24) | (res2[count - 251] << 16) | (res2[count - 250] << 8) | (res2[count - 249] << 0))) * 0.001;
            double active_mdi_neg_t3 = ((UInt32)((res2[count - 233] << 24) | (res2[count - 232] << 16) | (res2[count - 231] << 8) | (res2[count - 230] << 0))) * 0.001;
            double active_mdi_neg_t4 = ((UInt32)((res2[count - 214] << 24) | (res2[count - 213] << 16) | (res2[count - 212] << 8) | (res2[count - 211] << 0))) * 0.001;
            double active_mdi_neg_tl = ((UInt32)((res2[count - 195] << 24) | (res2[count - 194] << 16) | (res2[count - 193] << 8) | (res2[count - 192] << 0))) * 0.001;

            double active_mdi_abs_t1 = ((UInt32)((res2[count - 176] << 24) | (res2[count - 175] << 16) | (res2[count - 174] << 8) | (res2[count - 173] << 0))) * 0.001;
            double active_mdi_abs_t2 = ((UInt32)((res2[count - 157] << 24) | (res2[count - 156] << 16) | (res2[count - 155] << 8) | (res2[count - 154] << 0))) * 0.001;
            double active_mdi_abs_t3 = ((UInt32)((res2[count - 138] << 24) | (res2[count - 137] << 16) | (res2[count - 136] << 8) | (res2[count - 135] << 0))) * 0.001;
            double active_mdi_abs_t4 = ((UInt32)((res2[count - 119] << 24) | (res2[count - 118] << 16) | (res2[count - 117] << 8) | (res2[count - 116] << 0))) * 0.001;
            double active_mdi_abs_tl = ((UInt32)((res2[count - 100] << 24) | (res2[count - 99] << 16) | (res2[count - 98] << 8) | (res2[count - 97] << 0))) * 0.001;

            double cumulative_mdi_pos_t1 = ((UInt32)((data[count - 81] << 24) | (data[count - 80] << 16) | (data[count - 79] << 8) | (data[count - 78] << 0))) * 0.001;
            double cumulative_mdi_pos_t2 = ((UInt32)((data[count - 76] << 24) | (data[count - 75] << 16) | (data[count - 74] << 8) | (data[count - 73] << 0))) * 0.001;
            double cumulative_mdi_pos_t3 = ((UInt32)((data[count - 71] << 24) | (data[count - 70] << 16) | (data[count - 69] << 8) | (data[count - 68] << 0))) * 0.001;
            double cumulative_mdi_pos_t4 = ((UInt32)((data[count - 66] << 24) | (data[count - 65] << 16) | (data[count - 64] << 8) | (data[count - 63] << 0))) * 0.001;
            double cumulative_mdi_pos_tl = ((UInt32)((data[count - 61] << 24) | (data[count - 60] << 16) | (data[count - 59] << 8) | (data[count - 58] << 0))) * 0.001;

            double cumulative_mdi_neg_t1 = ((UInt32)((data[count - 56] << 24) | (data[count - 55] << 16) | (data[count - 54] << 8) | (data[count - 53] << 0))) * 0.001;
            double cumulative_mdi_neg_t2 = ((UInt32)((data[count - 51] << 24) | (data[count - 50] << 16) | (data[count - 49] << 8) | (data[count - 48] << 0))) * 0.001;
            double cumulative_mdi_neg_t3 = ((UInt32)((data[count - 46] << 24) | (data[count - 45] << 16) | (data[count - 44] << 8) | (data[count - 43] << 0))) * 0.001;
            double cumulative_mdi_neg_t4 = ((UInt32)((data[count - 41] << 24) | (data[count - 40] << 16) | (data[count - 39] << 8) | (data[count - 38] << 0))) * 0.001;
            double cumulative_mdi_neg_tl = ((UInt32)((data[count - 36] << 24) | (data[count - 35] << 16) | (data[count - 34] << 8) | (data[count - 33] << 0))) * 0.001;

            double cumulative_mdi_abs_t1 = ((UInt32)((data[count - 31] << 24) | (data[count - 30] << 16) | (data[count - 29] << 8) | (data[count - 28] << 0))) * 0.001;
            double cumulative_mdi_abs_t2 = ((UInt32)((data[count - 26] << 24) | (data[count - 25] << 16) | (data[count - 24] << 8) | (data[count - 23] << 0))) * 0.001;
            double cumulative_mdi_abs_t3 = ((UInt32)((data[count - 21] << 24) | (data[count - 20] << 16) | (data[count - 19] << 8) | (data[count - 18] << 0))) * 0.001;
            double cumulative_mdi_abs_t4 = ((UInt32)((data[count - 16] << 24) | (data[count - 15] << 16) | (data[count - 14] << 8) | (data[count - 13] << 0))) * 0.001;
            double cumulative_mdi_abs_tl = ((UInt32)((data[count - 11] << 24) | (data[count - 10] << 16) | (data[count - 9] << 8) | (data[count - 8] << 0))) * 0.001;

            double reset_count = (UInt32)((data[count - 6] << 24) | (data[count - 5] << 16) | (data[count - 4] << 8) | (data[count - 3] << 0));
            stream.Write(blk_2, 0, blk_2.Length);
            count = stream.Read(data, 0, data.Length);
            for (int i = 0; i < count; i++)
            {
                res3.Add(data[i]);
            }

            UInt16 mdi_reset_y = (UInt16)((res3[count - 12] << 8) | (res3[count - 11] << 0));
            string mdi_reset_datetime = mdi_reset_y + "/" + res3[count - 10] + "/" + res3[count - 9] + " " + res3[count - 7] + ":" + res3[count - 6] + ":" + res3[count - 5];

            ExecuteNonQurey("INSERT IGNORE INTO meter.monthly_billing_data_udil4 VALUES('" + MSN + "', '" + global_device_id + "', '" + date_time_b1 + "', '" + active_energy_pos_t1 + "', '" + active_energy_pos_t2 + "', '" + active_energy_pos_t3 + "', '" + active_energy_pos_t4 + "', '" + active_energy_pos_tl + "', '" + active_energy_neg_t1 + "', '" + active_energy_neg_t2 + "', '" + active_energy_neg_t3 + "', '" + active_energy_neg_t4 + "', '" + active_energy_neg_tl + "', '" + reactive_energy_pos_t1 + "', '" + reactive_energy_pos_t2 + "', '" + reactive_energy_pos_t3 + "', '" + reactive_energy_pos_t4 + "', '" + reactive_energy_pos_tl + "', '" + reactive_energy_neg_t1 + "', '" + reactive_energy_neg_t2 + "', '" + reactive_energy_neg_t3 + "', '" + reactive_energy_neg_t4 + "', '" + reactive_energy_neg_tl + "', '" + active_mdi_pos_t1 + "', '" + active_mdi_pos_t2 + "', '" + active_mdi_pos_t3 + "', '" + active_mdi_pos_t4 + "', '" + active_mdi_pos_tl + "', '" + active_mdi_neg_t1 + "', '" + active_mdi_neg_t2 + "', '" + active_mdi_neg_t3 + "', '" + active_mdi_neg_t4 + "', '" + active_mdi_neg_tl + "','" + active_mdi_abs_t1 + "', '" + active_mdi_abs_t2 + "', '" + active_mdi_abs_t3 + "', '" + active_mdi_abs_t4 + "', '" + active_mdi_abs_tl + "', '" + cumulative_mdi_pos_t1 + "', '" + cumulative_mdi_pos_t2 + "', '" + cumulative_mdi_pos_t3 + "', '" + cumulative_mdi_pos_t4 + "', '" + cumulative_mdi_pos_tl + "', '" + cumulative_mdi_neg_t1 + "', '" + cumulative_mdi_neg_t2 + "', '" + cumulative_mdi_neg_t3 + "', '" + cumulative_mdi_neg_t4 + "', '" + cumulative_mdi_neg_tl + "','" + cumulative_mdi_abs_t1 + "', '" + cumulative_mdi_abs_t2 + "', '" + cumulative_mdi_abs_t3 + "', '" + cumulative_mdi_abs_t4 + "', '" + cumulative_mdi_abs_tl + "', '" + mdi_reset_datetime + "', '" + reset_count + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");
            ExecuteNonQurey("UPDATE meter.meter SET monthly_bill_read = 0 WHERE global_device_id = '" + global_device_id + "';");

            return 0;
        }
        private int read_event_data(NetworkStream stream, byte[] data, UInt32 MSN, string global_device_id)
        {
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine(DateTime.Now.ToString() + ": MSN " + MSN + " Event Read Started");

            int count = 0;
            byte index_blk = 0;
            int index = 20;
            byte[] event_by_time = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 7, 1, 0, 99, 98, 0, 255, 2, 0 };
            byte[] next_block = { 0, 1, 0, 48, 0, 1, 0, 7, 192, 2, 129, 0, 0, 0, 1 };
            List<byte> all_event_data = new List<byte>();
            stream.Write(event_by_time, 0, event_by_time.Length);
            count = stream.Read(data, 0, data.Length);
            bool flagging = true;
            do
            {
                if (data[8] == 14)
                {
                    throw new Exception("Event Read Error.");
                }

                if (data[11] == 255)
                {
                    index = 19;
                }

                for (int i = index; i < count; i++)
                {
                    all_event_data.Add(data[i]);
                }

                if (data[11] == 01)
                {
                    flagging = false;
                    break;
                }
                index_blk++;
                next_block[next_block.Length - 1] = index_blk;
                stream.Write(next_block, 0, next_block.Length);
                count = stream.Read(data, 0, data.Length);
                count++;
            }
            while (flagging);

            for (int i = 0; i < all_event_data.Count - 28; i++)
            {
                if ((all_event_data[i] == 2) && (all_event_data[i + 1] == 2) && (all_event_data[i + 2] == 16))
                {
                    UInt16 event_code = (UInt16)((all_event_data[i + 3] << 8) | (all_event_data[i + 4] << 0));
                    UInt16 s_year = (UInt16)((all_event_data[i + 7] << 8) | (all_event_data[i + 8] << 0));
                    string s_date_time = s_year + "/" + all_event_data[i + 9] + "/" + all_event_data[i + 10] + " " + all_event_data[i + 12] + ":" + all_event_data[i + 13] + ":" + all_event_data[i + 14];
                    if (all_event_data[i + 12] == 255)
                    {
                        break;
                    }
                    if (event_code != 0)
                    {

                        ExecuteNonQurey("INSERT INTO meter.eventlist (msn,eventCode,Time_stamp)VALUES (" + MSN + ",'" + event_code + "','" + s_date_time + "');");
                        
                        ExecuteNonQurey("INSERT INTO meter.events1 VALUES('" + MSN + "', '" + global_device_id + "', '" + s_date_time + "', '" + event_code + "', '" + eventCodeTranslation(event_code.ToString()) + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");
                        ExecuteNonQurey("INSERT INTO meter.recentevents VALUES('" + MSN + "', '" + event_code + "', '" + s_date_time + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");
                    }

                    if (!(all_event_data[i + 26] == 255 || all_event_data[i + 27] == 255 || all_event_data[i + 29]==0 || all_event_data[i + 31] ==255))
                    {
                        UInt16 e_year = (UInt16)((all_event_data[i + 26] << 8) | (all_event_data[i + 27] << 0));
                        string e_date_time = e_year + "/" + all_event_data[i + 28] + "/" + all_event_data[i + 29] + " " + all_event_data[i + 31] + ":" + all_event_data[i + 32] + ":" + all_event_data[i + 33];
                        string end_event_code = event_end_code_map(event_code.ToString());
                        if (end_event_code != "000")
                        {
                            ExecuteNonQurey("INSERT INTO meter.eventlist (msn,eventCode,Time_stamp)VALUES (" + MSN + ",'" + end_event_code + "','" + e_date_time + "');");

                            ExecuteNonQurey("INSERT INTO meter.events1 VALUES('" + MSN + "', '" + global_device_id + "', '" + e_date_time + "', '" + end_event_code + "', '" + eventCodeTranslation(end_event_code) + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");
                            ExecuteNonQurey("INSERT INTO meter.recentevents VALUES('" + MSN + "', '" + end_event_code + "', '" + s_date_time + "', '"  +e_date_time + "');");
                        }
                    }
                    i += 28;
                }
            }
            ExecuteNonQurey("UPDATE meter.meter SET event_read = 0 WHERE global_device_id = '" + global_device_id + "';");
            Console.WriteLine(DateTime.Now.ToString() + ": MSN " + MSN + " Event Read Complete");
            Console.WriteLine("--------------------------------------------------------------");
            return 0;
        }
        private bool EstablishAARQ(bool aarq, NetworkStream stream)
        {
            if (!aarq)
            {
                int count = 0;
                byte[] data = new byte[1000];
                byte[] aarq_request = { 0, 1, 0, 48, 0, 1, 0, 56, 96, 54, 161, 9, 6, 7, 96, 133, 116, 5, 8, 1, 1, 138, 2, 7, 128, 139, 7, 96, 133, 116, 5, 8, 2, 1, 172, 10, 128, 8, 49, 50, 51, 52, 53, 54, 55, 56, 190, 16, 4, 14, 1, 0, 0, 0, 6, 95, 31, 4, 0, 0, 126, 31, 4, 176 };
                stream.Write(aarq_request, 0, aarq_request.Length);
                count = stream.Read(data, 0, data.Length);

                if (!(data[count - 1] == 0x07 && data[count - 2] == 0x00 && data[count - 3] == 0x2C))
                {
                    //stream.Write(cosem_release, 0, cosem_release.Length);
                    //count = stream.Read(data, 0, data.Length);

                    stream.Write(aarq_request, 0, aarq_request.Length);
                    count = stream.Read(data, 0, data.Length);

                    if (!(data[count - 1] == 0x07 && data[count - 2] == 0x00 && data[count - 3] == 0x2C))
                    {
                        throw new Exception("Association Faliure");
                    }
                }
            }
            return true;
        }
        private int LPCollect(NetworkStream stream, byte[] data, UInt32 MSN, DateTime Now, string global_device_id)
        {
            DataTable DT = new DataTable();
            DateTime dt_prev_ch1 = DateTime.Now.AddHours(-24);
            DateTime dt_prev_ch2 = DateTime.Now.AddHours(-24);

            DT = ExecuteReader("SELECT meter_datetime FROM meter.load_profile_ch1 WHERE msn = '" + MSN + "' ORDER BY meter_datetime desc limit 1;");
            foreach (DataRow dr in DT.Rows)
            {
                dt_prev_ch1 = (DateTime)(dr["meter_datetime"]);
            }

            DT = ExecuteReader("SELECT meter_datetime FROM meter.load_profile_ch2 WHERE msn = '" + MSN + "' ORDER BY meter_datetime desc limit 1;");
            foreach (DataRow dr in DT.Rows)
            {
                dt_prev_ch2 = (DateTime)(dr["meter_datetime"]);
            }

            /*
            if((DateTime.Now - dt_prev).Hours > 24)
            {
                dt_prev = DateTime.Now.AddHours(-24);
            }*/

            int count = 0;
            byte[] year_ch1 = BitConverter.GetBytes(Convert.ToInt32(dt_prev_ch1.ToString("yyyy")));
            byte[] year_ch2 = BitConverter.GetBytes(Convert.ToInt32(dt_prev_ch2.ToString("yyyy")));

            string read_loadprofile_by_range_ch1 = "00 01 00 30 00 01 00 40 C0 01 81 00 07 01 00 63 01 00 FF 02 01 01 02 04 02 04 12 00 08 09 06 00 00 01 00 00 FF 0F 02 12 00 00 09 0C " + year_ch1[1].ToString("X2") + " " + year_ch1[0].ToString("X2") + " " + dt_prev_ch1.Month.ToString("X2") + " " + dt_prev_ch1.Day.ToString("X2") + " 05 " + dt_prev_ch1.Hour.ToString("X2") + " " + dt_prev_ch1.Minute.ToString("X2") + " " + dt_prev_ch1.Second.ToString("X2") + " 00 80 00 00 09 0C " + year_ch1[1].ToString("X2") + " " + year_ch1[0].ToString("X2") + " " + DateTime.Now.Month.ToString("X2") + " " + DateTime.Now.Day.ToString("X2") + " 05 10 05 00 00 80 00 00 01 00";
            byte[] _read_loadprofile_by_range_ch1 = read_loadprofile_by_range_ch1.Split().Select(s => Convert.ToByte(s, 16)).ToArray();


            byte[] next_block = "00 01 00 30 00 01 00 07 C0 02 81 00 00 00 01".Split().Select(s => Convert.ToByte(s, 16)).ToArray();

            Console.WriteLine(MSN + ": Ch1 Reading...");
            stream.Write(_read_loadprofile_by_range_ch1, 0, _read_loadprofile_by_range_ch1.Length);
            //DebugData(_read_loadprofile_by_range_ch1,_read_loadprofile_by_range_ch1.Length);
            count = stream.Read(data, 0, data.Length);
            Console.WriteLine(MSN + ": Response...");
            //DebugData(data,count);

            byte index_blk = 0;
            List<byte> all_data_ch1 = new List<byte>();
            List<byte> all_data_ch2 = new List<byte>();

            bool showstopper = true;

            do
            {
                if (data[8] == 14)
                {
                    break;
                    //throw new Exception(DateTime.Now.ToString() + ": " + MSN + " Data Error: LP Read");
                }

                for (int i = 0; i < count; i++)
                {
                    if ((data[i] == 2) && (data[i + 1] == 9) && (data[i + 2] == 9) && (data[i + 3] == 12))
                    {
                        for (int j = i - 2; j < count; j++)
                        {
                            all_data_ch1.Add(data[j]);
                        }
                        break;
                    }
                }

                if (data[11] == 255) { break; }

                if (count > 250)
                {
                    index_blk++;
                    next_block[next_block.Length - 1] = index_blk;

                    stream.Write(next_block, 0, next_block.Length);
                    Console.WriteLine(MSN + " - Blk:" + index_blk);
                    //DebugData(next_block, next_block.Length);
                    count = stream.Read(data, 0, data.Length);
                    Console.WriteLine(MSN + ": Blk Response");
                    //DebugData(data, count);
                }
                else
                {
                    break;
                }
            } while (showstopper);

            showstopper = true;
            index_blk = 0;
            next_block[next_block.Length - 1] = index_blk;

            Console.WriteLine(MSN + ": Ch2 Reading...");
            string read_loadprofile_by_range_ch2 = "00 01 00 30 00 01 00 40 C0 01 81 00 07 01 00 63 01 01 FF 02 01 01 02 04 02 04 12 00 08 09 06 00 00 01 00 00 FF 0F 02 12 00 00 09 0C " + year_ch2[1].ToString("X2") + " " + year_ch2[0].ToString("X2") + " " + dt_prev_ch2.Month.ToString("X2") + " " + dt_prev_ch2.Day.ToString("X2") + " 03 " + dt_prev_ch2.Hour.ToString("X2") + " " + dt_prev_ch2.Minute.ToString("X2") + " " + dt_prev_ch2.Second.ToString("X2") + " 00 80 00 00 09 0C " + year_ch2[1].ToString("X2") + " " + year_ch2[0].ToString("X2") + " " + DateTime.Now.Month.ToString("X2") + " " + DateTime.Now.Day.ToString("X2") + " 05 10 05 00 00 80 00 00 01 00";
            byte[] _read_loadprofile_by_range_ch2 = read_loadprofile_by_range_ch2.Split().Select(s => Convert.ToByte(s, 16)).ToArray();

            stream.Write(_read_loadprofile_by_range_ch2, 0, _read_loadprofile_by_range_ch2.Length);
            //DebugData(_read_loadprofile_by_range_ch2, _read_loadprofile_by_range_ch2.Length);
            count = stream.Read(data, 0, data.Length);
            Console.WriteLine(MSN + ": Response...");
            //DebugData(data, count);

            do
            {
                if (data[8] == 14)
                {
                    DebugData(data, count);
                    break;
                    throw new Exception(DateTime.Now.ToString() + ": " + MSN + " Data Error: LP Read");
                }

                for (int i = 0; i < count; i++)
                {
                    if ((data[i] == 2) && (data[i + 1] == 9) && (data[i + 2] == 9) && (data[i + 3] == 12))
                    {
                        for (int j = i - 2; j < count; j++)
                        {
                            all_data_ch2.Add(data[j]);
                        }
                        break;
                    }
                }

                if (data[11] == 255) { break; }

                if (count > 250)
                {
                    index_blk++;
                    next_block[next_block.Length - 1] = index_blk;

                    stream.Write(next_block, 0, next_block.Length);
                    Console.WriteLine(MSN + " - Blk:" + index_blk);
                    //DebugData(next_block, next_block.Length);
                    count = stream.Read(data, 0, data.Length);
                    Console.WriteLine(MSN + ": Blk Response");
                    //DebugData(data, count);
                }
                else
                {
                    break;
                }
            }
            while (showstopper);

            Console.WriteLine(DateTime.Now.ToString() + ": LP Read Complete");
            for (int i = 0; i < all_data_ch1.Count - 55; i++)
            {
                Console.WriteLine(all_data_ch1.Count + "- iterator: " + i);
                if ((all_data_ch1[i] == 2) && (all_data_ch1[i + 1] == 9) && (all_data_ch1[i + 2] == 9) && (all_data_ch1[i + 3] == 12))
                {
                    UInt16 year_b1 = (UInt16)((all_data_ch1[i + 4] << 8) | (all_data_ch1[i + 5] << 0));
                    string date_time_b1 = year_b1 + "/" + all_data_ch1[i + 6] + "/" + all_data_ch1[i + 7] + " " + all_data_ch1[i + 9] + ":" + all_data_ch1[i + 10] + ":" + all_data_ch1[i + 11];

                    double active_energy_pos_tl = ((UInt32)((all_data_ch1[i + 17] << 24) | (all_data_ch1[i + 18] << 16) | (all_data_ch1[i + 19] << 8) | (all_data_ch1[i + 20] << 0))) * 0.001;
                    double active_energy_pos_t1 = ((UInt32)((all_data_ch1[i + 22] << 24) | (all_data_ch1[i + 23] << 16) | (all_data_ch1[i + 24] << 8) | (all_data_ch1[i + 25] << 0))) * 0.001;
                    double active_energy_pos_t2 = ((UInt32)((all_data_ch1[i + 27] << 24) | (all_data_ch1[i + 28] << 16) | (all_data_ch1[i + 29] << 8) | (all_data_ch1[i + 30] << 0))) * 0.001;

                    double reactive_energy_pos_tl = ((UInt32)((all_data_ch1[i + 32] << 24) | (all_data_ch1[i + 33] << 16) | (all_data_ch1[i + 34] << 8) | (all_data_ch1[i + 35] << 0))) * 0.001;
                    double reactive_energy_pos_t1 = ((UInt32)((all_data_ch1[i + 37] << 24) | (all_data_ch1[i + 38] << 16) | (all_data_ch1[i + 39] << 8) | (all_data_ch1[i + 40] << 0))) * 0.001;
                    double reactive_energy_pos_t2 = ((UInt32)((all_data_ch1[i + 42] << 24) | (all_data_ch1[i + 43] << 16) | (all_data_ch1[i + 44] << 8) | (all_data_ch1[i + 45] << 0))) * 0.001;

                    double aggregate_active_pwr_pos = ((UInt32)((all_data_ch1[i + 47] << 24) | (all_data_ch1[i + 48] << 16) | (all_data_ch1[i + 49] << 8) | (all_data_ch1[i + 50] << 0))) * 0.001;
                    double aggregate_reactive_pwr_pos = ((UInt32)((all_data_ch1[i + 52] << 24) | (all_data_ch1[i + 53] << 16) | (all_data_ch1[i + 54] << 8) | (all_data_ch1[i + 55] << 0))) * 0.001;

                    ExecuteNonQurey("INSERT IGNORE INTO meter.load_profile_ch1 VALUES('" + MSN + "', '" + global_device_id + "', '" + date_time_b1 + "', '" + active_energy_pos_t1 + "', '" + active_energy_pos_t2 + "', '" + active_energy_pos_tl + "', '" + reactive_energy_pos_t1 + "', '" + reactive_energy_pos_t2 + "', '" + reactive_energy_pos_tl + "', '" + aggregate_active_pwr_pos + "', '" + aggregate_reactive_pwr_pos + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");

                    i += 55;
                }
            }

            for (int i = 0; i < all_data_ch2.Count - 55; i++)
            {
                Console.WriteLine(all_data_ch2.Count + "- iterator: " + i);
                if ((all_data_ch2[i] == 2) && (all_data_ch2[i + 1] == 9) && (all_data_ch2[i + 2] == 9) && (all_data_ch2[i + 3] == 12))
                {
                    UInt16 year_b1 = (UInt16)((all_data_ch2[i + 4] << 8) | (all_data_ch2[i + 5] << 0));
                    string date_time_b1 = year_b1 + "/" + all_data_ch2[i + 6] + "/" + all_data_ch2[i + 7] + " " + all_data_ch2[i + 9] + ":" + all_data_ch2[i + 10] + ":" + all_data_ch2[i + 11];

                    double frequency = ((UInt32)((all_data_ch2[i + 17] << 24) | (all_data_ch2[i + 18] << 16) | (all_data_ch2[i + 19] << 8) | (all_data_ch2[i + 20] << 0))) * 0.01;
                    double pf = ((UInt32)((all_data_ch2[i + 22] << 24) | (all_data_ch2[i + 23] << 16) | (all_data_ch2[i + 24] << 8) | (all_data_ch2[i + 25] << 0))) * 0.001;
                    double voltageA = ((UInt32)((all_data_ch2[i + 27] << 24) | (all_data_ch2[i + 28] << 16) | (all_data_ch2[i + 29] << 8) | (all_data_ch2[i + 30] << 0))) * 0.01;
                    double voltageB = ((UInt32)((all_data_ch2[i + 32] << 24) | (all_data_ch2[i + 33] << 16) | (all_data_ch2[i + 34] << 8) | (all_data_ch2[i + 35] << 0))) * 0.01;
                    double voltageC = ((UInt32)((all_data_ch2[i + 37] << 24) | (all_data_ch2[i + 38] << 16) | (all_data_ch2[i + 39] << 8) | (all_data_ch2[i + 40] << 0))) * 0.01;
                    double CurrentA = ((UInt32)((all_data_ch2[i + 42] << 24) | (all_data_ch2[i + 43] << 16) | (all_data_ch2[i + 44] << 8) | (all_data_ch2[i + 45] << 0))) * 0.001;
                    double CurrentB = ((UInt32)((all_data_ch2[i + 47] << 24) | (all_data_ch2[i + 48] << 16) | (all_data_ch2[i + 49] << 8) | (all_data_ch2[i + 50] << 0))) * 0.001;
                    double CurrentC = ((UInt32)((all_data_ch2[i + 52] << 24) | (all_data_ch2[i + 53] << 16) | (all_data_ch2[i + 54] << 8) | (all_data_ch2[i + 55] << 0))) * 0.001;

                    ExecuteNonQurey("INSERT IGNORE INTO meter.load_profile_ch2 VALUES('" + MSN + "', '" + global_device_id + "', '" + date_time_b1 + "', '" + frequency + "', '" + pf + "', '" + CurrentA + "', '" + CurrentB + "', '" + CurrentC + "', '" + voltageA + "', '" + voltageB + "', '" + voltageC + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");
                    i += 55;
                }
            }

            ExecuteNonQurey("UPDATE meter.meter SET loadprofile = 0 WHERE global_device_id = '" + global_device_id + "';");
            return 0;
        }
        private int instantaneous_Collecter(NetworkStream stream, byte[] data, UInt32 MSN, DateTime Now, string global_device_id)
        {
            string min = DateTime.Now.Minute.ToString();
            int x = Int16.Parse(min);
            x = x - 4;
            min = x.ToString("X2");
            int count = 0;
            string Instantaneous_by_range_ch1 = "00 01 00 30 00 01 00 40 C0 01 81 00 07 01 00 63 01 01 FF 02 01 01 02 04 02 04 12 00 08 09 06 00 00 01 00 00 FF 0F 02 12 00 00 09 0C 07 E6 " + DateTime.Now.Month.ToString("X2") + " " + DateTime.Now.Day.ToString("X2") + " 03 " + DateTime.Now.Hour.ToString("X2") + " " + min + " " + DateTime.Now.Second.ToString("X2") + " 00 80 00 00 09 0C 07 E6" + " " + DateTime.Now.Month.ToString("X2") + " " + DateTime.Now.Day.ToString("X2") + " 03 " + DateTime.Now.Hour.ToString("X2") + " " + DateTime.Now.Minute.ToString("X2") + " " + DateTime.Now.Second.ToString("X2") + " 00 80 00 00 01 00";

            byte[] _Instantaneous_by_range_ch1 = Instantaneous_by_range_ch1.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
            stream.Write(_Instantaneous_by_range_ch1, 0, _Instantaneous_by_range_ch1.Length);
            Console.WriteLine("\n" + "MeterSerialNumber : " + MSN.ToString() + "Channel 2 Reading");
            DebugData(_Instantaneous_by_range_ch1, _Instantaneous_by_range_ch1.Length);

            count = stream.Read(data, 0, data.Length);
            Console.WriteLine(MSN + ": Response...");
            DebugData(data, count);

            if (data[9] == 01)
            {
                Console.WriteLine("Without block transfer");
            }


            UInt16 year_b1 = (UInt16)((data[18] << 8) | (data[19] << 0));
            string date_time_b1 = year_b1 + "/" + data[20] + "/" + data[21] + " " + data[23] + ":" + data[24] + ":" + data[25];

            double Aggregate_Active_power_Import = ((UInt32)((data[36] << 24) | (data[37] << 16) | (data[38] << 8) | (data[39] << 0))) * 0.001;
            double Aggregate_Active_power_Export = ((UInt32)((data[41] << 24) | (data[42] << 16) | (data[43] << 8) | (data[44] << 0))) * 0.001;
            double Aggregate_Active_power_absolute = ((UInt32)((data[46] << 24) | (data[47] << 16) | (data[48] << 8) | (data[49] << 0))) * 0.001;
            double Aggregate_Reactive_power_Import = ((UInt32)((data[51] << 24) | (data[52] << 16) | (data[53] << 8) | (data[54] << 0))) * 0.001;
            double Aggregate_Reactive_power_Export = ((UInt32)((data[56] << 24) | (data[57] << 16) | (data[58] << 8) | (data[59] << 0))) * 0.001;
            double Average_Power_Factor = ((UInt32)((data[64] << 24) | (data[65] << 16) | (data[66] << 8) | (data[67] << 0))) * 0.001;

            double Voltage_Phase_A = (UInt16)((data[69] << 8) | (data[70] << 0)) * 0.01;
            double Voltage_Phase_B = (UInt16)((data[72] << 8) | (data[73] << 0)) * 0.01;
            double Voltage_Phase_C = (UInt16)((data[75] << 8) | (data[76] << 0)) * 0.01;

            double Current_Phase_A = ((UInt32)((data[78] << 24) | (data[79] << 16) | (data[80] << 8) | (data[81] << 0))) * 0.001;
            double Current_Phase_B = ((UInt32)((data[83] << 24) | (data[84] << 16) | (data[85] << 8) | (data[86] << 0))) * 0.001;
            double Current_Phase_C = ((UInt32)((data[88] << 24) | (data[89] << 16) | (data[90] << 8) | (data[91] << 0))) * 0.001;

            double Interval = ((UInt32)((data[31] << 24) | (data[32] << 16) | (data[33] << 8) | (data[34] << 0))) * 0.001;
            double Frequency = (UInt16)((data[61] << 8) | (data[62] << 0)) * 0.01;
            double Aggregate_Reactive_power_Absolute = ((UInt32)((data[64] << 24) | (data[65] << 16) | (data[66] << 8) | (data[67] << 0))) * 0.001;

            //signal
            int Current_Active_Tariff = data[97];
            string sig = (data[95].ToString("X2"));
            int intAgain = int.Parse(sig, System.Globalization.NumberStyles.HexNumber);
            int b = 256;
            int c = b - intAgain;
            c = c * -1;
            sig = c.ToString() + " dbm";

            ExecuteNonQurey("insert into systempower(power, Time_stamp, rpower) values('" + Aggregate_Active_power_absolute + "', '" + date_time_b1 + "', '" + Aggregate_Reactive_power_Absolute + "');");
            ExecuteNonQurey("UPDATE `meter` SET latestkW ='" + Aggregate_Active_power_absolute + "' WHERE `serial` ='" + MSN + "';");
            ExecuteNonQurey("UPDATE `meter` SET latestkVAR ='" + Aggregate_Reactive_power_Absolute + "' WHERE `serial` ='" + MSN + "';");
            ExecuteNonQurey("INSERT INTO `meter`.`instantaneous_by_range`(`msn`,`global_device_id`,`meter_datetime`,`Interval`,`Aggregate_Active_power_Import`,`Aggregate_Active_power_Export`,`Aggregate_Active_power_absolute`,`Aggregate_Reactive_power_Absolute`,`Aggregate_Reactive_power_Export`,`Aggregate_Reactive_power_Import`,`Average_Power_Factor`,`Voltage_Phase_A`,`Voltage_Phase_B`,`Voltage_Phase_C`,`Current_Phase_A`,`Current_Phase_B`,`Current_Phase_C`,`freq`,`signal`,`Current_Active_Tariff`,`mdc_read_datetime`,`db_datetime`) VALUES('" + MSN + "', '" + global_device_id + "', '" + date_time_b1 + "', '" + Interval + "', '" + Aggregate_Active_power_Import + "', '" + Aggregate_Active_power_Export + "', '" + Aggregate_Active_power_absolute + "', '" + Aggregate_Reactive_power_Import + "', '" + Aggregate_Reactive_power_Export + "', '" + Aggregate_Reactive_power_Absolute + "', '" + Average_Power_Factor + "', '" + Voltage_Phase_A + "', '" + Voltage_Phase_B + "', '" + Voltage_Phase_C + "', '" + Current_Phase_A + "', '" + Current_Phase_B + "', '" + Current_Phase_C + "', '" + Frequency + "', '" + sig + "', '" + Current_Active_Tariff + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "'); ");
            ExecuteNonQurey("UPDATE meter.meter SET instantaneous = 0 WHERE global_device_id = '" + global_device_id + "';");
            return 0;
        }
        private void LoadProfileReadTime_Elapsed(Object source, ElapsedEventArgs e)
        {
            try
            {
                DBGetSet db = new DBGetSet();
                DataTable dt = new DataTable();
                DateTime DT = DateTime.Now;
                int hour = DT.Hour;
                int minute = DT.Minute;
                string[] serials = new string[10000];
                int count = 0;

                DateTime stTim = DateTime.Now.AddHours(-2);
                DateTime endTim = DateTime.Now.AddHours(-1);

                if ((hour == 23 && (minute >= 45 && minute < 46)) || (hour == 12 && (minute >= 0 && minute < 1)))
                {
                    db.Query = "select latestkW,latestkVAR from meter where connected = 1;";
                    dt = db.ExecuteReader();
                    float sum = 0, sum1 = 0;
                    foreach (DataRow dr in dt.Rows)
                    {
                        sum += float.Parse(dr["latestkW"].ToString());
                        sum1 += float.Parse(dr["latestkVAR"].ToString());
                    }
                    db.Query = "insert into systempower(power,Time_stamp,rpower) values('" + sum + "','" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "','" + sum1 + "')";
                    db.ExecuteNonQuery();

                    ExecuteNonQurey("UPDATE meter.meter SET loadprofile = 1, instantaneous = 1;");
                }

                if ((hour == 21 && (minute >= 1 && minute < 2)) || (hour == 9 && (minute >= 1 && minute < 2)))
                {
                    ExecuteNonQurey("UPDATE meter SET job = 1, daily_bill_read = 1;");
                }

                if (hour == 0 && (minute >= 1 && minute < 2))
                {
                    ExecuteNonQurey("UPDATE meter SET monthly_bill_read = 1, event_read = 1, sync = 1;");
                }
            }
            catch (Exception ex)
            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                Console.WriteLine(ex.Message);
            }
        }
        private void mode2Initiate_Elapsed(Object source, ElapsedEventArgs e)
        {
            try
            {
                byte[] aarq_request = { 0, 1, 0, 48, 0, 1, 0, 56, 96, 54, 161, 9, 6, 7, 96, 133, 116, 5, 8, 1, 1, 138, 2, 7, 128, 139, 7, 96, 133, 116, 5, 8, 2, 1, 172, 10, 128, 8, 49, 50, 51, 52, 53, 54, 55, 56, 190, 16, 4, 14, 1, 0, 0, 0, 6, 95, 31, 4, 0, 0, 126, 31, 4, 176 };
                List<UInt32> msnList = new List<UInt32>();
                int count = 0;
                List<UInt32> trylist = new List<UInt32>();
                using (OdbcConnection connection = new OdbcConnection(myconn))
                {
                    connection.Open();
                    using (OdbcCommand command = new OdbcCommand("SELECT `serial`, try FROM meter.meter WHERE (connected = 1 and mode = 1 and reading = 0) and (`read` = 1 or firmware = 1 or retry = 1 or configure = 1 or demand = 1 or relayDCRequest = 1 or relayReconnect = 1 or demandrs = 1 or readdemand = 1);", connection))
                    using (OdbcDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            msnList.Add(UInt32.Parse(dr["serial"].ToString()));
                            trylist.Add(UInt32.Parse(dr["try"].ToString()));
                            count++;
                        }
                        dr.Close();
                    }
                    connection.Close();
                }
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        if (Stream_Object_Dict.ContainsKey(msnList[i]) && Stream_Object_Dict[msnList[i]].CanWrite)
                        {
                            Stream_Object_Dict[msnList[i]].Write(aarq_request, 0, aarq_request.Length);
                            Console.WriteLine(DateTime.Now.ToString() + " - " + msnList[i] + ": Initiating On Demand Action....");
                        }
                        else
                        {
                            Console.WriteLine(DateTime.Now.ToString() + " - " + msnList[i] + ": On Demand Action Terminated. Device connection lost with Server.");
                            //ExecuteNonQurey("update meter.meter set connected = 0 where `serial` = '" + msnList[i] + "';");
                        }
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine(msnList[i] + " - " + DateTime.Now.ToString() + ": " + exp.Message);
                        continue;
                    }

                    if (trylist[i] > 8)
                    {
                        ExecuteNonQurey("UPDATE meter SET `read` = 0, demand = 0, retry=0, relayDCRequest = 0, relayReconnect=0, configure=0, firmware = 0, try = 0 where `serial` = " + msnList[i] + ";");
                    }
                    else
                    {
                        ExecuteNonQurey("UPDATE meter SET try = '" + (trylist[i] + 1) + "' where `serial` = " + msnList[i] + ";");
                    }
                }
            }
            catch (Exception ex)
            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                Console.WriteLine(ex.Message);
            }
        }
        private void ConnectionCheck_Elapsed(Object source, ElapsedEventArgs e)
        {
            try
            {
                List<string> msns = new List<string>();
                using (OdbcConnection connection = new OdbcConnection(myconn))
                {
                    connection.Open();
                    using (OdbcCommand command = new OdbcCommand("SELECT * FROM meter;", connection))
                    using (OdbcDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string timecomp = dr["times"].ToString();
                            DateTime comp = DateTime.Parse(timecomp);
                            TimeSpan diff = TimeSpan.Parse("00:10:20.9896330");
                            if (DateTime.Now.Subtract(comp) > diff)
                            {
                                msns.Add((string)dr["serial"]);
                            }
                        }
                        dr.Close();
                    }
                    connection.Close();
                }

                foreach (string i in msns)
                {
                    ExecuteNonQurey("UPDATE meter SET connected = 0 where msn='" + i + "' and mode = 1;");
                    ExecuteNonQurey("update meter set connected = 0 where lastRead < '" + DateTime.Now.AddDays(-1).ToString("yyyy/M/d HH:mm:ss") + "';");
                }
            }
            catch (Exception ex)
            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                Console.WriteLine(ex.Message);
            }
        }
        private void StartAccept()
        {
            try
            {
                server.BeginAcceptTcpClient(HandleAsyncConnection, server);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void HandleAsyncConnection(IAsyncResult res)
        {
            try
            {
                StartAccept(); //listen for new connections again
                TcpClient client = server.EndAcceptTcpClient(res);
                string remoteEndPoint = client.Client.RemoteEndPoint.ToString();
                //proceed
                this.BeginInvoke(new Action(() => {
                    try
                    {
                        label2.Text = remoteEndPoint + " @ " + DateTime.Now.ToString();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Client Gone wait please" + ex.ToString());
                    }
                }));
                string clientItem = DateTime.Now + ": " + remoteEndPoint;
                this.BeginInvoke(new Action(() => {
                    listBox_ConnectedClients.Items.Add(clientItem);
                }));
                HandleClient(client);
                this.BeginInvoke(new Action(() => {
                    listBox_ConnectedClients.Items.Remove(clientItem);
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR! HandleAsyncConnection: " + ex.Message);
            }
        }
        private string convertr(byte[] ndata)
        {
            string str2 = "";
            for (int index = 0; index < ndata.Length; ++index)
            {
                str2 = str2 + ndata[index].ToString("X2") + " ";
            }
            return str2;
        }
        private void HandleClient(object client)
        {
            using (TcpClient tcpClient = client as TcpClient)
            {
                using (NetworkStream stream = tcpClient.GetStream())
                {
                    string remoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
                    bool isitMode2 = true;
                    int faliures = 0;
                    UInt32 MSN = new UInt32();
                    string MSNType = "Three Phase";
                    do
                    {
                        /*******************Meter Visuals PITC**************************************/
                        MeterVisualsChecklist meter_visuals = new MeterVisualsChecklist();
                        meter_visuals.aggregate_active_pwr_pos_datetime = "2000/1/1 11:11:11";
                        meter_visuals.aggregate_reactive_pwr_pos_datetime = "2000/1/1 11:11:11";
                        meter_visuals.auxr_datetime = "2000/1/1 11:11:11";
                        meter_visuals.dmdt_datetime = "2000/1/1 11:11:11";
                        meter_visuals.dvtm_datetime = "2000/1/1 11:11:11";
                        meter_visuals.last_command_datetime = "2000/1/1 11:11:11";
                        meter_visuals.last_command_resp_datetime = "2000/1/1 11:11:11";
                        meter_visuals.last_communication_datetime = "2000/1/1 11:11:11";
                        meter_visuals.lsch_datetime = "2000/1/1 11:11:11";
                        meter_visuals.lsch_end_datetime = "2000/1/1 11:11:11";
                        meter_visuals.lsch_start_datetime = "2000/1/1 11:11:11";
                        meter_visuals.mdc_read_datetime = "2000/1/1 11:11:11";
                        meter_visuals.db_datetime = "2000/1/1 11:11:11";
                        meter_visuals.meter_datetime = "2000/1/1 11:11:11";
                        meter_visuals.mtst_datetime = "2000/1/1 11:11:11";
                        meter_visuals.power_status_datetime = "2000/1/1 11:11:11";
                        meter_visuals.sanc_datetime = "2000/1/1 11:11:11";
                        meter_visuals.dvtm_meter_clock = "2000/1/1 11:11:11";
                        meter_visuals.mdi_reset_date = 1;
                        /*******************Meter Visuals PITC**************************************/

                        double last_active_energy_pos_tl = 0;
                        string last_active_energy_pos_tl_datetime = "2000/1/1 11:11:11";
                        double last_active_energy_neg_tl = 0;
                        string last_active_energy_neg_tl_datetime = "2000/1/1 11:11:11";
                        double last_reactive_energy_pos_tl = 0;
                        double last_reactive_energy_neg_tl = 0;
                        string last_reactive_energy_pos_tl_datetime = "2000/1/1 11:11:11";
                        string last_reactive_energy_neg_tl_datetime = "2000/1/1 11:11:11";

                        string powerPlan = "";
                        string global_device_id = "";
                        bool planActive = false;
                        UInt32 maulTheConfigCommand = 0;
                        bool firmwareLoaded = false;
                        bool aarqStatus = false;
                        byte[] data = new byte[1024];
                        int count = 0;
                        byte[] RelayON = "00 01 00 30 00 01 00 0F C3 01 C1 00 46 00 00 60 03 0A FF 02 01 0F 00".Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                        byte[] RelayOff = "00 01 00 30 00 01 00 0F C3 01 C1 00 46 00 00 60 03 0A FF 01 01 0F 00".Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                        byte[] relayOutputStatus = "00 01 00 30 00 01 00 0D C0 01 C1 00 46 00 00 60 03 0A FF 02 00".Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                        bool flag = false;
                        bool wrapper = false;
                        int demandrs = 0;
                        int readdemand = 0;
                        string dsmplan = "";
                        int demand = 0;
                        int groupdemandmgmt = 0;
                        DataTable dt = new DataTable();
                        string transactionid = "";

                        int relayStatus = 0;
                        int relayDCRequest = 0;
                        int relayRecon = 0;

                        byte[] sig = new byte[16];
                        string msn = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 60 01 00 FF 02 00";
                        byte[] readSerial = msn.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                        try
                        {
                            MSN = PushEventsCheck(stream, remoteEndPoint, isitMode2, ref aarqStatus);
                            stream.ReadTimeout = 30000;
                            if (MSN == 0)
                            {
                                if (stream.DataAvailable)
                                {
                                    count = stream.Read(data, 0, data.Length);
                                }
                                aarqStatus = EstablishAARQ(aarqStatus, stream);
                                stream.Write(readSerial, 0, readSerial.Length);
                                count = stream.Read(data, 0, data.Length);

                                if (data[8] == 14)
                                {
                                    stream.Close();
                                    tcpClient.Close();
                                    return;
                                }
                                MSN = UInt32.Parse((data[count - 10] - 48).ToString() + (data[count - 9] - 48).ToString() + (data[count - 8] - 48).ToString() + (data[count - 7] - 48).ToString() + (data[count - 6] - 48).ToString() + (data[count - 5] - 48).ToString() + (data[count - 4] - 48).ToString() + (data[count - 3] - 48).ToString() + (data[count - 2] - 48).ToString() + (data[count - 1] - 48).ToString());
                                wrapper = true;
                                this.BeginInvoke(new Action(() => {

                                    labelClient.Text = MSN.ToString();
                                }));
                            }
                            this.BeginInvoke(new Action(() => {
                                try
                                {
                                    label2.Text = tcpClient.Client.RemoteEndPoint.ToString() + " @ " + DateTime.Now.ToString();
                                }
                                catch (Exception ex)
                                {
                                    string message = ex.ToString() + "\n";
                                    String response = "\nClient Gone: " + "Exception: " + message;
                                }
                            }));
                            this.BeginInvoke(new Action(() => {

                                labelContime.Show();
                            }));
                            this.BeginInvoke(new Action(() => {
                                labelContime.Text = DateTime.Now.ToLongTimeString();
                            }));

                            if (MSN > 1 && MSN < 4200000000)
                            {
                                wrapper = true;
                            }
                            this.BeginInvoke(new Action(() => {
                                labelClient.Text = MSN.ToString();
                            }));
                            if (MSN < 1)
                            {
                                stream.Close();
                                tcpClient.Close();
                                return;
                            }
                            aarqStatus = EstablishAARQ(aarqStatus, stream);

                            Console.WriteLine(MSN + " :Session Started.");
                            ExecuteNonQurey("UPDATE meter SET connected ='1', times ='" + DateTime.Now.ToString() + "' WHERE `serial` ='" + MSN + "';");

                            int isSupposedToRead = 0;
                            string readType = "";
                            int instantaaa = 0;
                            int comm = 0, theMode = 0;
                            float latitude = 44;
                            float longitude = 45;
                            int n = 0;
                            int t = 0, syncer = 0, firmware = 0, loadprofileer = 0;
                            int configure = 0;
                            string cu = "";
                            int daily_bill_read = 0, monthly_bill_read = 0, event_read = 0, alarm_read = 0;

                            if (wrapper)
                            {
                                this.BeginInvoke(new Action(() => {
                                    labelClient.Show();
                                    labelClient.Text = MSN.ToString();
                                }));
                                maulTheConfigCommand = MSN;
                                /****************** Loading Visuals ******************************/
                                meter_visuals.msn = MSN;
                                dt = ExecuteReader("select * from meter.meter_snapshot where msn = '" + MSN + "';");
                                foreach (DataRow dr in dt.Rows)
                                {
                                    meter_visuals.aggregate_active_pwr_pos = double.Parse(dr["aggregate_active_pwr_pos"].ToString());
                                    meter_visuals.aggregate_active_pwr_pos_datetime = DateTime.Parse(dr["aggregate_active_pwr_pos_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.aggregate_reactive_pwr_pos = double.Parse(dr["aggregate_reactive_pwr_pos"].ToString());
                                    meter_visuals.aggregate_reactive_pwr_pos_datetime = DateTime.Parse(dr["aggregate_reactive_pwr_pos_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.auxr_datetime = DateTime.Parse(dr["auxr_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.auxr_status = dr["auxr_status"].ToString();
                                    meter_visuals.average_pf = double.Parse(dr["average_pf"].ToString());
                                    meter_visuals.current_phase_a = double.Parse(dr["current_phase_a"].ToString());
                                    meter_visuals.current_phase_b = double.Parse(dr["current_phase_b"].ToString());
                                    meter_visuals.current_phase_c = double.Parse(dr["current_phase_c"].ToString());
                                    meter_visuals.dmdt_bidirectional_device = dr["dmdt_bidirectional_device"].ToString();
                                    meter_visuals.dmdt_communication_interval = dr["dmdt_communication_interval"].ToString();
                                    meter_visuals.dmdt_communication_mode = dr["dmdt_communication_mode"].ToString();
                                    meter_visuals.dmdt_communication_type = dr["dmdt_communication_type"].ToString();
                                    meter_visuals.dmdt_datetime = DateTime.Parse(dr["dmdt_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.dmdt_meter_type = dr["dmdt_meter_type"].ToString();
                                    meter_visuals.dmdt_phase = dr["dmdt_phase"].ToString();
                                    meter_visuals.dvtm_datetime = DateTime.Parse(dr["dvtm_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.dvtm_meter_clock = DateTime.Parse(dr["dvtm_meter_clock"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.frequency = double.Parse(dr["frequency"].ToString());
                                    meter_visuals.global_device_id = dr["global_device_id"].ToString();
                                    meter_visuals.last_command = dr["last_command"].ToString();
                                    meter_visuals.last_command_datetime = DateTime.Parse(dr["last_command_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.last_command_resp = dr["last_command_resp"].ToString();
                                    meter_visuals.last_command_resp_datetime = DateTime.Parse(dr["last_command_resp_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.last_communication_datetime = DateTime.Parse(dr["last_communication_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.last_signal_strength = double.Parse(dr["last_signal_strength"].ToString());
                                    meter_visuals.lsch_datetime = DateTime.Parse(dr["lsch_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.lsch_end_datetime = DateTime.Parse(dr["lsch_end_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.lsch_load_shedding_slabs = dr["lsch_load_shedding_slabs"].ToString();
                                    meter_visuals.lsch_start_datetime = DateTime.Parse(dr["lsch_start_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.mdi_reset_date = int.Parse(dr["mdi_reset_date"].ToString());
                                    meter_visuals.mdi_reset_time = dr["mdi_reset_time"].ToString();
                                    meter_visuals.meter_datetime = DateTime.Parse(dr["meter_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.msn = uint.Parse(dr["msn"].ToString());
                                    meter_visuals.mtst_datetime = DateTime.Parse(dr["mtst_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.mtst_meter_activation_status = int.Parse(dr["mtst_meter_activation_status"].ToString());
                                    meter_visuals.power_status = int.Parse(dr["power_status"].ToString());
                                    meter_visuals.power_status_datetime = DateTime.Parse(dr["power_status_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.sanc_datetime = DateTime.Parse(dr["sanc_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                    meter_visuals.sanc_load_limit = dr["sanc_load_limit"].ToString();
                                    meter_visuals.sanc_maximum_retries = dr["sanc_maximum_retries"].ToString();
                                    meter_visuals.sanc_retry_clear_interval = dr["sanc_retry_clear_interval"].ToString();
                                    meter_visuals.sanc_retry_interval = dr["sanc_retry_interval"].ToString();
                                    meter_visuals.sanc_threshold_duration = dr["sanc_threshold_duration"].ToString();
                                    meter_visuals.voltage_phase_a = double.Parse(dr["voltage_phase_a"].ToString());
                                    meter_visuals.voltage_phase_b = double.Parse(dr["voltage_phase_b"].ToString());
                                    meter_visuals.voltage_phase_c = double.Parse(dr["voltage_phase_c"].ToString());
                                }
                                string checker = "";
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                                string checker1 = "";
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                                dt = ExecuteReader("SELECT * FROM meter where serial = '" + MSN + "';");
                                foreach (DataRow dr in dt.Rows)
                                {
                                    flag = true;
                                    global_device_id = (string)dr["global_device_id"];
                                    checker = (string)dr["serial"];
                                    t = (int)dr["job"];
                                    isSupposedToRead = int.Parse(dr["read"].ToString());
                                    readType = (string)(dr["readType"]);
                                    comm = (int)(dr["retry"]);
                                    theMode = int.Parse(dr["mode"].ToString());
                                    latitude = (float)dr["lat"];
                                    longitude = (float)dr["longituge"];
                                    instantaaa = int.Parse(dr["instantaneous"].ToString());
                                    flag = true;
                                    MSNType = dr["deviceType"].ToString();
                                    demandrs = int.Parse(dr["demandrs"].ToString());
                                    readdemand = int.Parse(dr["readdemand"].ToString());
                                    dsmplan = dr["demandmgmt"].ToString();
                                    groupdemandmgmt = int.Parse(dr["groupdemandmgmt"].ToString());
                                    global_device_id = dr["global_device_id"].ToString();
                                    meter_visuals.global_device_id = global_device_id;
                                    demand = int.Parse(dr["demand"].ToString());
                                    relayStatus = (int)dr["relayStatus"];
                                    relayDCRequest = int.Parse(dr["relayDCRequest"].ToString());
                                    relayRecon = int.Parse(dr["relayReconnect"].ToString());
                                    t = (int)dr["job"];
                                    instantaaa = int.Parse(dr["instantaneous"].ToString());
                                    syncer = int.Parse(dr["sync"].ToString());
                                    firmware = int.Parse(dr["firmware"].ToString());
                                    loadprofileer = int.Parse(dr["loadprofile"].ToString());
                                    isSupposedToRead = int.Parse(dr["read"].ToString());
                                    readType = (string)(dr["readType"]);
                                    cu = dr["customerCode"].ToString();
                                    configure = int.Parse(dr["configure"].ToString());
                                    n = (int)(dr["msn"]);
                                    comm = (int)(dr["retry"]);
                                    theMode = int.Parse(dr["mode"].ToString());
                                    meter_visuals.mtst_meter_activation_status = int.Parse(dr["meter_activation_status"].ToString());
                                    latitude = (float)dr["lat"];
                                    longitude = (float)dr["longituge"];
                                    daily_bill_read = int.Parse(dr["daily_bill_read"].ToString());
                                    monthly_bill_read = int.Parse(dr["monthly_bill_read"].ToString());
                                    event_read = int.Parse(dr["event_read"].ToString());
                                    alarm_read = int.Parse(dr["alarm_read"].ToString());
                                }
                                if (checker != MSN.ToString())
                                {
                                    ExecuteNonQurey("INSERT INTO `meter` (serial,connected,reading,times) VALUES (" + MSN + ",'" + '1' + "','" + '1' + "','" + DateTime.Now + "');");
                                    ExecuteNonQurey("INSERT INTO `meter` (read,count,retry,try) VALUES (" + '1' + ",'" + '1' + "','" + '1' + "','" + '1' + "');");
                                }
                                else
                                {
                                    ExecuteNonQurey("UPDATE meter SET global_device_id ='" + global_device_id + "' WHERE `serial` ='" + MSN + "';");
                                    ExecuteNonQurey("UPDATE meter SET times ='" + DateTime.Now.ToString() + "' WHERE `serial` ='" + MSN + "';");
                                    ExecuteNonQurey("UPDATE meter SET reading ='" + isSupposedToRead + "' WHERE `serial` ='" + MSN + "';");
                                    ExecuteNonQurey("UPDATE meter SET retry ='" + isSupposedToRead + "' WHERE `serial` ='" + MSN + "';");
                                    ExecuteNonQurey("UPDATE meter SET try ='" + isSupposedToRead + "' WHERE `serial` ='" + MSN + "';");
                                    ExecuteNonQurey("UPDATE meter SET lat ='" + latitude + "' WHERE `serial` ='" + MSN + "';");
                                    ExecuteNonQurey("UPDATE meter SET longituge ='" + longitude + "' WHERE `serial` ='" + MSN + "';");
                                    // ExecuteNonQurey("UPDATE meter SET job ='" + '1' + "' WHERE `serial` ='" + MSN + "';");
                                    isSupposedToRead = 1;

                                    Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Comm Session Started", DateTime.Now, 1);
                                    if (!flag)
                                    {
                                        int unreg = 0;
                                        dt = ExecuteReader("SELECT * FROM unregistered where serial ='" + MSN + "';");
                                        unreg = dt.Rows.Count;
                                        string stmp = String.Format("{0:yyyy/M/d HH:mm:ss}", DateTime.Now);
                                        if (unreg < 1)
                                        {
                                            ExecuteNonQurey("INSERT INTO unregistered (serial,Time_stamp) VALUES (" + MSN + ",'" + stmp + "');");
                                        }
                                        else
                                        {
                                            long indexer = 99999999999;
                                            dt = ExecuteReader("SELECT amount FROM unregistered where serial ='" + MSN + "';");
                                            foreach (DataRow dr in dt.Rows)
                                            {
                                                indexer = (long)(dr["amount"]);
                                                unreg++;
                                            }
                                            ExecuteNonQurey("UPDATE unregistered SET Time_stamp ='" + stmp + "' WHERE amount='" + indexer + "' and serial ='" + MSN + "';");
                                        }
                                        stream.Close();
                                        stream.Flush();
                                        tcpClient.Close();
                                        return;
                                    }
                                    if (autoAlarmEventRead || configure == 1 || readdemand == 1 || relayDCRequest == 1 || relayRecon == 1 || isSupposedToRead == 1 || syncer == 1 || comm == 1 || demand == 1 || t == 1 || instantaaa == 1 || demandrs == 1 || groupdemandmgmt == 1 || loadprofileer == 1 || firmware == 1 || daily_bill_read == 1 || monthly_bill_read == 1 || event_read == 1 || alarm_read == 1)
                                    {
                                        aarqStatus = EstablishAARQ(aarqStatus, stream);
                                        ExecuteNonQurey("UPDATE meter.meter SET reading ='1' WHERE serial ='" + MSN + "';");
                                    }
                                    if (isSupposedToRead == 1)
                                    {
                                        ExecuteNonQurey("UPDATE meter.meter SET retry ='1' WHERE serial ='" + MSN + "';");
                                        dt = ExecuteReader("SELECT * FROM meter where serial = '" + MSN + "';");
                                        foreach (DataRow dr in dt.Rows)
                                        {
                                            comm = (int)(dr["retry"]);

                                        }
                                    }
                                    if (!Stream_Object_Dict.ContainsKey(MSN))
                                    {
                                        Stream_Object_Dict.Add(MSN, stream);
                                    }
                                    else
                                    {
                                        Stream_Object_Dict[MSN] = stream;
                                    }
                                    /**********************************************************************/
                                    /**************************Comm Verification Portion***************************/


                                    if (comm == 1)
                                    {
                                        Console.WriteLine("-------------------------");
                                        Console.WriteLine("Comm Verification Started");
                                        byte[] Active_Energy = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 15, 8, 0, 255, 2, 0 };
                                        byte[] Reactive_Energy = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 128, 8, 0, 255, 2, 0 };
                                        byte[] Active_Power = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 15, 7, 0, 255, 2, 0 };
                                        byte[] Reactive_Instant = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 128, 7, 0, 255, 2, 0 };
                                        Console.WriteLine("Active Energy Command Sended");
                                        DebugData(Active_Energy, Active_Energy.Length);
                                        stream.Write(Active_Energy, 0, Active_Energy.Length);
                                        count = stream.Read(data, 0, data.Length);
                                        Console.WriteLine("Active Energy Command Reply");
                                        DebugData(data, count);
                                        if (data[8] == 14)
                                        {
                                            throw new Exception("Data Error in Scaler Read.");
                                        }

                                        double merg = (UInt32)((data[count - 4] << 24) | (data[count - 3] << 16) | (data[count - 2] << 8) | (data[count - 1] << 0));
                                        double commVerkWh = merg / 1000;
                                        Console.WriteLine("Reactive Energy Command Sended");
                                        DebugData(Reactive_Energy, Reactive_Energy.Length);
                                        stream.Write(Reactive_Energy, 0, Reactive_Energy.Length);
                                        Console.WriteLine("Reactive Energy Command Reply");

                                        count = stream.Read(data, 0, data.Length);

                                        DebugData(data, count);
                                        merg = (UInt32)((data[count - 4] << 24) | (data[count - 3] << 16) | (data[count - 2] << 8) | (data[count - 1] << 0));
                                        double commVerkVARh = merg / 1000;
                                        string dutttame = String.Format("{0:yyyy/M/d HH:mm:ss}", DateTime.Now);


                                        stream.Write(Active_Power, 0, Active_Power.Length);

                                        count = stream.Read(data, 0, data.Length);
                                        merg = (UInt32)((data[count - 4] << 24) | (data[count - 3] << 16) | (data[count - 2] << 8) | (data[count - 1] << 0));
                                        double kW = merg / 1000;

                                        stream.Write(Reactive_Instant, 0, Reactive_Instant.Length);

                                        count = stream.Read(data, 0, data.Length);
                                        merg = (UInt32)((data[count - 4] << 24) | (data[count - 3] << 16) | (data[count - 2] << 8) | (data[count - 1] << 0));
                                        double kVAR = merg / 1000;
                                        ExecuteNonQurey("INSERT INTO `meter`.`loadprofile` (serial,kWh,kW,kVARh,kVAR,Time_stamp) values('" + MSN + "','" + commVerkWh + "','" + kW + "','" + commVerkVARh + "','" + kVAR + "','" +dutttame + "');");

                                        ExecuteNonQurey("UPDATE meter SET latestkW ='" + kW + "' WHERE `serial` ='" + MSN + "';");
                                        ExecuteNonQurey("UPDATE meter SET latestkVAR ='" + kVAR + "' WHERE `serial` ='" + MSN + "';");
                                        ExecuteNonQurey("UPDATE meter SET latestkWh ='" + commVerkWh + "' WHERE `serial` ='" + MSN + "';");
                                        ExecuteNonQurey("INSERT INTO `systempower`(power,Time_stamp,rpower) values('" + commVerkWh + "','" + dutttame + "','" + commVerkVARh + "');");
                                        ExecuteNonQurey("INSERT INTO `comm-verification` (serial,active,reactive,Time_stamp) VALUES (" + MSN + ",'" + commVerkWh + "','" + commVerkVARh + "','" + dutttame + "');");

                                        ExecuteNonQurey("update meter.meter set retry = 0 where `serial` = '" + MSN + "';");
                                        Console.WriteLine("Comm Verification Ended");
                                        Console.WriteLine("-------------------------");
                                    }
                                    
                                       // Console.WriteLine("Point2");
                                        byte[] customerCode = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 60 01 0A FF 02 00".Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                        stream.Write(customerCode, 0, customerCode.Length);
                                        count = stream.Read(data, 0, data.Length);
                                        if (data[8] == 14)
                                        {
                                            throw new Exception("Data Error in Customer Code Read");
                                        }
                                        string cuscode = (data[count - 8] - 48).ToString() + (data[count - 7] - 48).ToString() + (data[count - 6] - 48).ToString() + (data[count - 5] - 48).ToString() + (data[count - 4] - 48).ToString() + (data[count - 3] - 48).ToString() + (data[count - 2] - 48).ToString() + (data[count - 1] - 48).ToString();
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            tbActivePower.Text = cuscode;
                                        }));
                                        decimal c2=update_rssi_value(MSN,stream,data);
                                        Console.WriteLine("Signal Strength of"+MSN+"=" + c2);
                                        //Signal in DBM ///
                                        decimal SignalStrength = c2;
                                        /// Reading at Instant//
                                        string Instant = "00 01 00 30 00 01 00 0D C0 01 81 00 07 01 00 5E 5C 0A FF 02 00";
                                        byte[] InstantByte = Instant.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                        stream.Write(InstantByte, 0, InstantByte.Length);
                                        count = stream.Read(data, 0, data.Length);
                                        Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reading Instantaneous Data", DateTime.Now, 1);
                                        string currentTarrrr = data[count - 1].ToString();
                                        float AggReactivePowerExport = (UInt32)((data[count - 6] << 24) | (data[count - 5] << 16) | (data[count - 4] << 8) | (data[count - 3] << 0));
                                        AggReactivePowerExport = AggReactivePowerExport / 1000;
                                        float AggRactivePowerImport = (UInt32)((data[count - 11] << 24) | (data[count - 10] << 16) | (data[count - 9] << 8) | (data[count - 8] << 0));
                                        AggRactivePowerImport = AggRactivePowerImport / 1000;
                                        float AggActivePowerExport = (UInt32)((data[count - 16] << 24) | (data[count - 15] << 16) | (data[count - 14] << 8) | (data[count - 13] << 0));
                                        AggActivePowerExport = AggActivePowerExport / 1000;
                                        float AggActivePowerImport = (UInt32)((data[count - 21] << 24) | (data[count - 20] << 16) | (data[count - 19] << 8) | (data[count - 18] << 0));
                                        AggActivePowerImport = AggActivePowerImport / 1000;
                                        float FrequencyInstantaneous = (UInt16)((data[count - 24] << 8) | (data[count - 23] << 0));
                                        FrequencyInstantaneous = FrequencyInstantaneous / 100;
                                        float PowerFactor = (UInt16)((data[count - 27] << 8) | (data[count - 26] << 0));
                                        PowerFactor = PowerFactor / 1000;
                                        float VoltageC = (UInt16)((data[count - 39] << 8) | (data[count - 38] << 0));
                                        VoltageC = VoltageC / 100;
                                        float VoltageB = (UInt16)((data[count - 42] << 8) | (data[count - 41] << 0));
                                        VoltageB = VoltageB / 100;
                                        float VoltageA = (UInt16)((data[count - 45] << 8) | (data[count - 44] << 0));
                                        VoltageA = VoltageA / 100;
                                        float CurrentC = (UInt32)((data[count - 50] << 24) | (data[count - 49] << 16) | (data[count - 48] << 8) | (data[count - 47] << 0));
                                        CurrentC = CurrentC / 1000;
                                        float CurrentB = (UInt32)((data[count - 55] << 24) | (data[count - 54] << 16) | (data[count - 53] << 8) | (data[count - 52] << 0));
                                        CurrentB = CurrentB / 1000;
                                        float CurrentA = (UInt32)((data[count - 60] << 24) | (data[count - 59] << 16) | (data[count - 58] << 8) | (data[count - 57] << 0));
                                        CurrentA = CurrentA / 1000;
                                        string DateTimeInstantaneous = ((UInt16)((data[count - 73] << 8) | (data[count - 72] << 0))).ToString() + "/" + data[count - 71] + "/" + data[count - 70] + " " + data[count - 68] + ":" + data[count - 67] + ":" + data[count - 66];
                                        DateTime DTInst = DateTime.Parse(DateTimeInstantaneous);
                                        Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reading Instantaneous Data", DTInst, 1);
                                        //Adding Readings to Database
                                        string meterDateTimeInst = String.Format("{0:yyyy/M/d HH:mm:ss}", DateTime.Now);
                                        string stamp = String.Format("{0:yyyy/M/d HH:mm:ss}", DateTime.Now);
                                        instantaaa = 1;
                                        ExecuteNonQurey("update transaction_status1 set status_level='4', status_4_datetime = '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "' where transactionid = '" + transactionid + "' and msn='" + MSN + "';");
                                        ExecuteNonQurey("insert into random_instantaneous_data(current_tariff_register, signal_strength, msn, global_device_id, meter_datetime, current_phase_a, current_phase_b, current_phase_c, voltage_phase_a, voltage_phase_b, voltage_phase_c, aggregate_active_pwr_pos, aggregate_active_pwr_neg, aggregate_reactive_pwr_pos, aggregate_reactive_pwr_neg, average_pf, mdc_read_datetime) values('" + currentTarrrr + "', '" + SignalStrength + "', '" + MSN + "', '" + global_device_id + "', '" + meterDateTimeInst + "', '" + CurrentA + "', '" + CurrentB + "', '" + CurrentC + "', '" + VoltageA + "', '" + VoltageB + "', '" + VoltageC + "', '" + AggActivePowerImport + "', '" + AggActivePowerExport + "', '" + AggRactivePowerImport + "', '" + AggReactivePowerExport + "', '" + PowerFactor + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");

                                        /***************** PITC Data*************/
                                        meter_visuals.aggregate_active_pwr_pos = double.Parse(AggActivePowerImport.ToString());
                                        //meter_visuals.aggregate_active_pwr_neg = AggActivePowerExport;
                                        //meter_visuals.aggregate_reactive_pwr_neg = AggReactivePowerExport;
                                        meter_visuals.aggregate_reactive_pwr_pos = double.Parse(AggRactivePowerImport.ToString());

                                        //meter_visuals.aggregate_active_pwr_neg_datetime = String.Format("{0:yyyy/M/d HH:mm:ss}", DT);
                                        meter_visuals.aggregate_active_pwr_pos_datetime = String.Format("{0:yyyy/M/d HH:mm:ss}", DT);
                                        //meter_visuals.aggregate_reactive_pwr_neg_datetime = String.Format("{0:yyyy/M/d HH:mm:ss}", DT);
                                        meter_visuals.aggregate_reactive_pwr_pos_datetime = String.Format("{0:yyyy/M/d HH:mm:ss}", DT);

                                        //meter_visuals.meter_tariff_identification = "Tariff " + currentTarrrr;
                                        //meter_activation_status = 1;

                                        /***************** PITC Data**************/

                                        /**************************** Agg Active Pwr***************************/
                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.1.7.0.255','" + AggActivePowerImport + "','kW','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }

                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.2.7.0.255','" + AggActivePowerExport + "','kW','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                        /**********************************************************************/

                                        /**************************agg Reactive Pow***************************/
                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.3.7.0.255','" + AggRactivePowerImport + "','kW','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }

                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.4.7.0.255','" + AggReactivePowerExport + "','kW','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                        /********************************************************************/

                                        /*********************** Voltages *************************************/
                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.32.7.0.255','" + VoltageA + "','V','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }

                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.52.7.0.255','" + VoltageB + "','V','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }

                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.72.7.0.255','" + VoltageC + "','V','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                        /**********************************************************************/

                                        /********************** Currents ****************************************/
                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.31.7.0.255','" + CurrentA + "','A','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }

                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.51.7.0.255','" + CurrentB + "','A','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }

                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.71.7.0.255','" + CurrentC + "','A','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                        /***********************************************************************/

                                        /*************************** PF***************************************/
                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.13.7.0.255','" + PowerFactor + "','N/A','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                        /************************************************************************/

                                        /***************************  Freq ***************************************/
                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.14.7.0.255','" + FrequencyInstantaneous + "','Hz','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                        /************************************************************************/

                                        /***************************  Time Date ***************************************/
                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.0.9.1.255','0','" + DTInst.ToString("HH:mm:ss") + "','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }

                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.0.9.2.255','0','" + DTInst.ToString("yy/MM/dd") + "','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                        /************************************************************************/

                                        /***************************  Current Tariff ***************************************/
                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','0.0.96.14.0.255','" + currentTarrrr + "','N/A','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                        /************************************************************************/

                                        /***************************  CT Ratio ***************************************/
                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.0.4.2.255','1','N/A','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }

                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.0.4.5.255','1','N/A','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                        /************************************************************************/

                                        /***************************  PT Ratio ***************************************/
                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.0.4.3.255','1','N/A','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }

                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                        {
                                            connection.Open();
                                            using (OdbcCommand command = new OdbcCommand("INSERT INTO info1 (msn,OBIS,Amount,unit,Time_stamp,quantity,id) VALUES ('" + n + "','1.0.0.4.6.255','1','N/A','" + stamp + "',1,'" + MSN + "');", connection))
                                                command.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                        /*************************** Updation Start ***************************/
                                        meter_visuals.last_communication_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                        if (true)
                                        {
                                            dt = ExecuteReader("select * from transaction_status1 where msn='" + MSN + "' and completed = 0 order by command_receiving_datetime limit 1;");
                                            foreach (DataRow dr in dt.Rows)
                                            {
                                                aarqStatus = EstablishAARQ(aarqStatus, stream);
                                                transactionid = dr["transactionid"].ToString();
                                                string typ = dr["type"].ToString();
                                                Console.WriteLine(DateTime.Now.ToString() + " - " + MSN + ": " + typ);
                                                switch (typ)
                                                {
                                                    case "RCONFIG":
                                                        Console.WriteLine(MSN + ": Initiating Relay Config");
                                                        meter_visuals.last_command = "SANC Load Control Program";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        configure_relay(stream, MSN, data);
                                                        meter_visuals.sanc_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "SANC Programming", DateTime.Now, 1);
                                                        Console.WriteLine(MSN + ": Relay Config Complete");
                                                        meter_visuals.last_command_resp = "SANC Load set Successfully";
                                                        meter_visuals.last_command_resp_datetime = DateTime.Now.AddSeconds(3).ToString("yyyy/M/d HH:mm:ss");
                                                        break;

                                                    case "Relay":
                                                        if (dr["type_parameters"].ToString() == "0")
                                                        {
                                                            relayDCRequest = 1;
                                                        }
                                                        else if (dr["type_parameters"].ToString() == "1")
                                                        {
                                                            relayRecon = 1;
                                                        }
                                                        break;

                                                    case "Sync":
                                                        syncer = 1;
                                                        break;

                                                    case "EVNT":
                                                        meter_visuals.last_command = "Event Read";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        event_read = read_event_data(stream, data, MSN, global_device_id);
                                                        break;

                                                    case "INST":
                                                        meter_visuals.last_command = "INST Read";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        instantaaa = 1;
                                                        break;

                                                    case "BILL":
                                                        meter_visuals.last_command = "BILLING READ CURRENT";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        t = 1;
                                                        break;

                                                    case "MBIL":
                                                        meter_visuals.last_command = "Monthly Bill Read";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        monthly_bill_read = 1;
                                                        break;

                                                    case "LPRO":
                                                        meter_visuals.last_command = "Load Profile read";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        loadprofileer = 1;
                                                        break;

                                                    case "AUXR":
                                                        meter_visuals.last_command = "Check AUXR Status";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        relayStatus = check_relay_status(stream, MSN, data);
                                                        break;

                                                    case "DVTM":
                                                        meter_visuals.last_command = "DVTM Check";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        instantaaa = 1;
                                                        break;

                                                    case "SANC":
                                                        meter_visuals.last_command = "SANC Load Control Read";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        readType = "relay";
                                                        isSupposedToRead = 1;
                                                        break;

                                                    case "LSCH":
                                                        meter_visuals.last_command = "LSCH Read";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        readdemand = 1;
                                                        break;

                                                    case "MTST":
                                                        break;

                                                    case "DMDT":
                                                        meter_visuals.last_command = "DMDT Check";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        break;

                                                    case "Device Creation":
                                                        meter_visuals.last_command = "Device Creation";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        Console.WriteLine(DateTime.Now.ToString() + " MSN:" + MSN + " - Configuring Device");
                                                        theMode = initial_device_configuration(stream, MSN, global_device_id);
                                                        Console.WriteLine(DateTime.Now.ToString() + " MSN:" + MSN + " - Configuration Complete");
                                                        Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Device Creation Setup", DateTime.Now, 1);
                                                        isitMode2 = theMode == 1 ? true : false;
                                                        meter_visuals.last_command_resp = "Device Created Successfully";
                                                        meter_visuals.last_command_resp_datetime = DateTime.Now.AddSeconds(2).ToString("yyyy/M/d HH:mm:ss");
                                                        break;

                                                    case "LSCH_SET":
                                                        meter_visuals.last_command = "Demand Management Schedule";
                                                        meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                        meter_visuals.last_command_resp = "Schedule set Successfully";
                                                        meter_visuals.last_command_resp_datetime = DateTime.Now.AddSeconds(3).ToString("yyyy/M/d HH:mm:ss");
                                                        break;

                                                    default:
                                                        break;
                                                }
                                            }

                                            ExecuteNonQurey("update transaction_status1 set status_level='3', status_1_datetime = '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', status_2_datetime = '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', status_3_datetime ='" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "' where transactionid = '" + transactionid + "' and msn='" + MSN + "';");
                                        }

                                        if (/*demandrs == 1 || groupdemandmgmt == 1*/
                                        true)
                                        {
                                            dt = ExecuteReader("SELECT * FROM meter.lsch where msn = '" + MSN + "';");
                                            foreach (DataRow dr in dt.Rows)
                                            {
                                                meter_visuals.lsch_start_datetime = DateTime.Parse(dr["start_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                                meter_visuals.lsch_end_datetime = DateTime.Parse(dr["end_datetime"].ToString()).ToString("yyyy/M/d HH:mm:ss");
                                                meter_visuals.lsch_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                int LSCH_Programemd = int.Parse(dr["programmed"].ToString());
                                                bool LSCH_reset = false;
                                                if (DateTime.Now > DateTime.Parse(dr["start_datetime"].ToString()) && DateTime.Now < DateTime.Parse(dr["end_datetime"].ToString()))
                                                {
                                                    if (LSCH_Programemd == 1)
                                                    {
                                                        Console.WriteLine(MSN + ": LSCH Active");
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (LSCH_Programemd == 1)
                                                    {
                                                        Console.WriteLine(MSN + ": LSCH Resetting");
                                                        LSCH_reset = true;
                                                    }
                                                    else
                                                    {
                                                        break;
                                                    }
                                                }

                                                Console.WriteLine(MSN + ": Reset- " + LSCH_reset);
                                                string alt_slab = ConvertToX4ByteString(0xFFFF);

                                                string t1 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t1"].ToString())));
                                                string s1 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s1"].ToString())));
                                                string t2 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t2"].ToString())));
                                                string s2 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s2"].ToString())));
                                                string t3 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t3"].ToString())));
                                                string s3 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s3"].ToString())));
                                                string t4 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t4"].ToString())));
                                                string s4 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s4"].ToString())));
                                                string t5 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t5"].ToString())));
                                                string s5 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s5"].ToString())));
                                                string t6 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t6"].ToString())));
                                                string s6 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s6"].ToString())));
                                                string t7 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t7"].ToString())));
                                                string s7 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s7"].ToString())));
                                                string t8 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t8"].ToString())));
                                                string s8 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s8"].ToString())));
                                                string t9 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t9"].ToString())));
                                                string s9 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s9"].ToString())));
                                                string t10 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t10"].ToString())));
                                                string s10 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s10"].ToString())));
                                                string t11 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t11"].ToString())));
                                                string s11 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s11"].ToString())));
                                                string t12 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t12"].ToString())));
                                                string s12 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s12"].ToString())));
                                                string t13 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t13"].ToString())));
                                                string s13 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s13"].ToString())));
                                                string t14 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t14"].ToString())));
                                                string s14 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s14"].ToString())));
                                                string t15 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t15"].ToString())));
                                                string s15 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s15"].ToString())));
                                                string t16 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t16"].ToString())));
                                                string s16 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s16"].ToString())));
                                                string t17 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t17"].ToString())));
                                                string s17 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s17"].ToString())));
                                                string t18 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t18"].ToString())));
                                                string s18 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s18"].ToString())));
                                                string t19 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t19"].ToString())));
                                                string s19 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s19"].ToString())));
                                                string t20 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t20"].ToString())));
                                                string s20 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s20"].ToString())));
                                                string t21 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t21"].ToString())));
                                                string s21 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s21"].ToString())));
                                                string t22 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t22"].ToString())));
                                                string s22 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s22"].ToString())));
                                                string t23 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t23"].ToString())));
                                                string s23 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s23"].ToString())));
                                                string t24 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["t24"].ToString())));
                                                string s24 = LSCH_reset == true ? alt_slab : ConvertToX4ByteString(int.Parse(ParseInt(dr["s24"].ToString())));

                                                string setdem = "00 01 00 30 00 01 00 CF C1 01 81 00 01 01 00 5E 5C 0B FF 02 00 01 18 02 02 12 " + t1 + " 12 " + s1 + " 02 02 12 " + t2 + " 12 " + s2 + " 02 02 12 " + t3 + " 12 " + s3 + " 02 02 12 " + t4 + " 12 " + s4 + " 02 02 12 " + t5 + " 12 " + s5 + " 02 02 12 " + t6 + " 12 " + s6 + " 02 02 12 " + t7 + " 12 " + s7 + " 02 02 12 " + t8 + " 12 " + s8 + " 02 02 12 " + t9 + " 12 " + s9 + " 02 02 12 " + t10 + " 12 " + s10 + " 02 02 12 " + t11 + " 12 " + s11 + " 02 02 12 " + t12 + " 12 " + s12 + " 02 02 12 " + t13 + " 12 " + s13 + " 02 02 12 " + t14 + " 12 " + s14 + " 02 02 12 " + t15 + " 12 " + s15 + " 02 02 12 " + t16 + " 12 " + s16 + " 02 02 12 " + t17 + " 12 " + s17 + " 02 02 12 " + t18 + " 12 " + s18 + " 02 02 12 " + t19 + " 12 " + s19 + " 02 02 12 " + t20 + " 12 " + s20 + " 02 02 12 " + t21 + " 12 " + s21 + " 02 02 12 " + t22 + " 12 " + s22 + " 02 02 12 " + t23 + " 12 " + s23 + " 02 02 12 " + t24 + " 12 " + s24 + "";
                                                byte[] bytessetdem = setdem.Split().Select(s => Convert.ToByte(s, 16)).ToArray();

                                                //Console.WriteLine(setdem);

                                                meter_visuals.last_command = "Demand Management Schedule";
                                                meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                aarqStatus = EstablishAARQ(aarqStatus, stream);
                                                stream.Write(bytessetdem, 0, bytessetdem.Length);
                                                count = stream.Read(data, 0, data.Length);
                                                if (data[8] == 14)
                                                {
                                                    throw new Exception(DateTime.Now.ToString() + " " + MSN + ": LSCH Setting Error");
                                                }
                                                Console.WriteLine(MSN + ": LSCH Programming - " + LSCH_reset);
                                                meter_visuals.last_command_resp = "Schedule set Successfully";
                                                meter_visuals.last_command_resp_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                //if (data[count - 1] != 0) { last_command_resp = "Demand Mgmt Configuration Faliure"; throw new Exception(MSN + " Demand Mgmt Configuration Faliure"); }
                                                Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "LSCH Programmed", DateTime.Now, 1);
                                                if (LSCH_reset)
                                                {
                                                    ExecuteNonQurey("UPDATE meter.lsch set programmed = 0 where MSN = '" + MSN + "';");
                                                }
                                                else
                                                {
                                                    ExecuteNonQurey("UPDATE meter.lsch set programmed = 1 where MSN = '" + MSN + "';");
                                                }
                                            }
                                        }

                                        if (readdemand == 1)
                                        {
                                            meter_visuals.last_command = "Demand Management Schedule Read";
                                            meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                            meter_visuals.lsch_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                           // Console.WriteLine(MSN + ": Reading LSCH");
                                            string readdem = "00 01 00 30 00 01 00 0D C0 01 81 00 01 01 00 5E 5C 0B FF 02 00";
                                            byte[] bytesreadem = readdem.Split().Select(s => Convert.ToByte(s, 16)).ToArray();

                                            stream.Write(bytesreadem, 0, bytesreadem.Length);
                                            count = stream.Read(data, 0, data.Length);
                                            if (data[8] == 14)
                                            {
                                                throw new Exception(DateTime.Now.ToString() + " " + MSN + ": LSCH Read Error");
                                            }

                                            int t1 = ((data[17] << 8) | (data[18] << 0));
                                            int s1 = ((data[20] << 8) | (data[21] << 0));
                                            int t2 = ((data[25] << 8) | (data[26] << 0));
                                            int s2 = ((data[28] << 8) | (data[29] << 0));
                                            int t3 = ((data[33] << 8) | (data[34] << 0));
                                            int s3 = ((data[36] << 8) | (data[37] << 0));
                                            int t4 = ((data[41] << 8) | (data[42] << 0));
                                            int s4 = ((data[44] << 8) | (data[45] << 0));
                                            int t5 = ((data[49] << 8) | (data[50] << 0));
                                            int s5 = ((data[52] << 8) | (data[53] << 0));
                                            int t6 = ((data[57] << 8) | (data[58] << 0));
                                            int s6 = ((data[60] << 8) | (data[61] << 0));
                                            int t7 = ((data[65] << 8) | (data[66] << 0));
                                            int s7 = ((data[68] << 8) | (data[69] << 0));
                                            int t8 = ((data[73] << 8) | (data[74] << 0));
                                            int s8 = ((data[76] << 8) | (data[77] << 0));
                                            int t9 = ((data[81] << 8) | (data[82] << 0));
                                            int s9 = ((data[84] << 8) | (data[85] << 0));
                                            int t10 = ((data[89] << 8) | (data[90] << 0));
                                            int s10 = ((data[92] << 8) | (data[93] << 0));
                                            int t11 = ((data[97] << 8) | (data[98] << 0));
                                            int s11 = ((data[100] << 8) | (data[101] << 0));
                                            int t12 = ((data[105] << 8) | (data[106] << 0));
                                            int s12 = ((data[108] << 8) | (data[109] << 0));
                                            int t13 = ((data[113] << 8) | (data[114] << 0));
                                            int s13 = ((data[116] << 8) | (data[117] << 0));
                                            int t14 = ((data[121] << 8) | (data[122] << 0));
                                            int s14 = ((data[124] << 8) | (data[125] << 0));
                                            int t15 = ((data[129] << 8) | (data[130] << 0));
                                            int s15 = ((data[132] << 8) | (data[133] << 0));
                                            int t16 = ((data[137] << 8) | (data[138] << 0));
                                            int s16 = ((data[140] << 8) | (data[141] << 0));
                                            int t17 = ((data[145] << 8) | (data[146] << 0));
                                            int s17 = ((data[148] << 8) | (data[149] << 0));
                                            int t18 = ((data[153] << 8) | (data[154] << 0));
                                            int s18 = ((data[156] << 8) | (data[157] << 0));
                                            int t19 = ((data[161] << 8) | (data[162] << 0));
                                            int s19 = ((data[164] << 8) | (data[165] << 0));
                                            int t20 = ((data[169] << 8) | (data[170] << 0));
                                            int s20 = ((data[172] << 8) | (data[173] << 0));
                                            int t21 = ((data[177] << 8) | (data[178] << 0));
                                            int s21 = ((data[180] << 8) | (data[181] << 0));
                                            int t22 = ((data[185] << 8) | (data[186] << 0));
                                            int s22 = ((data[188] << 8) | (data[189] << 0));
                                            int t23 = ((data[193] << 8) | (data[194] << 0));
                                            int s23 = ((data[196] << 8) | (data[197] << 0));
                                            int t24 = ((data[201] << 8) | (data[202] << 0));
                                            int s24 = ((data[204] << 8) | (data[205] << 0));

                                            DateTime ls_st_dt = DateTime.Parse("2000/1/1 11:11:11");
                                            DateTime ls_end_dt = DateTime.Parse("2000/1/1 11:11:11");
                                            var lsch_set = "1";
                                            dt = ExecuteReader("select start_datetime, end_datetime, programmed from meter.lsch where msn = '" + MSN + "';");
                                            foreach (DataRow dr in dt.Rows)
                                            {
                                                ls_st_dt = DateTime.Parse(dr["start_datetime"].ToString());
                                                ls_end_dt = DateTime.Parse(dr["end_datetime"].ToString());
                                                lsch_set = dr["programmed"].ToString();

                                                meter_visuals.lsch_start_datetime = ls_st_dt.ToString("yyyy/M/d HH:mm:ss");
                                                meter_visuals.lsch_end_datetime = ls_end_dt.ToString("yyyy/M/d HH:mm:ss");
                                            }
                                            ExecuteNonQurey("replace into meter.lsch_read values('" + MSN + "', '" + global_device_id + "', '" + ls_st_dt.ToString("yyyy/M/d HH:mm:ss") + "', '" + ls_end_dt.ToString("yyyy/M/d HH:mm:ss") + "', '" + t1 + "', '" + s1 + "', '" + t2 + "', '" + s2 + "', '" + t3 + "', '" + s3 + "', '" + t4 + "', '" + s4 + "', '" + t5 + "', '" + s5 + "', '" + t6 + "', '" + s6 + "', '" + t7 + "', '" + s7 + "', '" + t8 + "', '" + s8 + "', '" + t9 + "', '" + s9 + "', '" + t10 + "', '" + s10 + "', '" + t11 + "', '" + s11 + "', '" + t12 + "', '" + s12 + "', '" + t13 + "', '" + s13 + "', '" + t14 + "', '" + s14 + "', '" + t15 + "', '" + s15 + "', '" + t16 + "', '" + s16 + "', '" + t17 + "', '" + s17 + "', '" + t18 + "', '" + s18 + "', '" + t19 + "', '" + s19 + "', '" + t20 + "', '" + s20 + "', '" + t21 + "', '" + s21 + "', '" + t22 + "', '" + s22 + "', '" + t23 + "', '" + s23 + "', '" + t24 + "', '" + s24 + "', '" + lsch_set + "');");

                                            dt = ExecuteReader("select id from randomread where id = '" + MSN + "' and type='dsm';");
                                            if (dt.Rows.Count > 0)
                                            {
                                                ExecuteNonQurey("update randomread set raw = '" + ConvertToByteString(data, count) + "' where id = '" + MSN + "';");
                                            }
                                            else
                                            {
                                                ExecuteNonQurey("insert into randomread(id,raw,type) values('" + MSN + "','" + ConvertToByteString(data, count) + "','dsm')");
                                            }
                                            meter_visuals.last_command_resp = "Schedule read Successful";
                                            meter_visuals.last_command_resp_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                        }

                                        if (flag)
                                        {
                                            if (MSNType == "Single Phase")
                                            {
                                                ExecuteNonQurey("UPDATE meter SET firmware ='0',instantaneous='0',loadprofile='0' WHERE serial ='" + MSN + "';");
                                            }

                                            using (OdbcConnection connection = new OdbcConnection(myconn))
                                            {
                                                connection.Open();
                                                using (OdbcCommand command = new OdbcCommand("SELECT msn,loadshedding,inloadshedding FROM meter WHERE serial = '" + MSN + "';", connection))
                                                using (OdbcDataReader dr = command.ExecuteReader())
                                                {
                                                    while (dr.Read())
                                                    {
                                                        n = (int)(dr["msn"]);
                                                        if (((int)(dr["inloadshedding"]) == 1))
                                                        {
                                                            planActive = true;
                                                        }
                                                        powerPlan = dr["loadshedding"].ToString();
                                                    }
                                                    dr.Close();
                                                }
                                                connection.Close();
                                            }

                                            /***************************** Firmware Upgarde***************************/
                                            /*************************************************************************/
                                            if (firmware == 1)
                                            {
                                                flag = false;
                                                string[] firmwarepackets = new string[1005];
                                                int totalsteps = 0;

                                                using (OdbcConnection connection = new OdbcConnection(myconn))
                                                {
                                                    connection.Open();
                                                    using (OdbcCommand command = new OdbcCommand("SELECT commands FROM firmware;", connection))
                                                    using (OdbcDataReader dr = command.ExecuteReader())
                                                    {
                                                        while (dr.Read())
                                                        {
                                                            firmwarepackets[totalsteps] = (string)(dr["commands"]);
                                                            totalsteps++;
                                                        }
                                                        dr.Close();
                                                    }
                                                    connection.Close();
                                                }

                                                meter_visuals.last_command = "Firmware Update";
                                                meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                for (int o = 0; o < totalsteps; o++)
                                                {
                                                    byte[] configcomm = firmwarepackets[o].Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                    string tu = "";
                                                    for (int r = 0; r < configcomm.Length; r++)
                                                    {
                                                        tu += configcomm[r].ToString("X2") + " ";
                                                    }
                                                    Console.WriteLine(tu);
                                                    stream.Write(configcomm, 0, configcomm.Length);
                                                    int roch = stream.Read(data, 0, data.Length);

                                                    tu = "";
                                                    for (int r = 0; r < roch; r++)
                                                    {
                                                        tu += data[r].ToString("X2") + " ";
                                                    }
                                                    Console.WriteLine(tu);
                                                }

                                                using (OdbcConnection connection = new OdbcConnection(myconn))
                                                {
                                                    connection.Open();
                                                    using (OdbcCommand command = new OdbcCommand("UPDATE meter SET firmware ='0' WHERE serial ='" + MSN + "';", connection))
                                                        command.ExecuteNonQuery();
                                                    connection.Close();
                                                }

                                                flag = false;
                                                firmwareLoaded = true;

                                                meter_visuals.last_command_resp = "Firmware Loaded Successfully";
                                                meter_visuals.last_command_resp_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                            }
                                            /********************************************Firmware************************************/
                                        }

                                        /**************************************Power Plan Adjustment*************************************************************/
                                        if (flag)
                                        {
                                            DateTime p1 = new DateTime();
                                            DateTime p11 = new DateTime();
                                            DateTime p2 = new DateTime();
                                            DateTime p22 = new DateTime();
                                            DateTime p3 = new DateTime();
                                            DateTime p33 = new DateTime();
                                            DateTime p4 = new DateTime();
                                            DateTime p44 = new DateTime();
                                            DateTime p5 = new DateTime();
                                            DateTime p55 = new DateTime();
                                            DateTime p6 = new DateTime();
                                            DateTime p66 = new DateTime();
                                            DateTime p7 = new DateTime();
                                            DateTime p77 = new DateTime();
                                            DateTime p8 = new DateTime();
                                            DateTime p88 = new DateTime();
                                            DateTime p9 = new DateTime();
                                            DateTime p99 = new DateTime();
                                            DateTime p10 = new DateTime();
                                            DateTime p100 = new DateTime();

                                            float powerValue = 69000;
                                            float thr1 = 69000;
                                            float thr2 = 69000;
                                            float thr3 = 69000;
                                            float thr4 = 69000;
                                            float thr5 = 69000;
                                            float thr6 = 69000;
                                            float thr7 = 69000;
                                            float thr8 = 69000;
                                            float thr9 = 69000;
                                            float thr10 = 69000;
                                            int rthr = 69000;
                                            bool planPresent = false;

                                            using (OdbcConnection connection = new OdbcConnection(myconn))
                                            {
                                                connection.Open();
                                                using (OdbcCommand command = new OdbcCommand("SELECT * FROM loadshedding  where id = '" + powerPlan + "';", connection))
                                                using (OdbcDataReader dr = command.ExecuteReader())
                                                {
                                                    while (dr.Read())
                                                    {
                                                        planPresent = true;
                                                        p1 = DateTime.Parse("4/7/2016 " + dr["t1"].ToString());
                                                        p11 = DateTime.Parse("4/7/2016 " + dr["t11"].ToString());
                                                        p2 = DateTime.Parse("4/7/2016 " + dr["t2"].ToString());
                                                        p22 = DateTime.Parse("4/7/2016 " + dr["t22"].ToString());
                                                        p3 = DateTime.Parse("4/7/2016 " + dr["t3"].ToString());
                                                        p33 = DateTime.Parse("4/7/2016 " + dr["t33"].ToString());
                                                        p4 = DateTime.Parse("4/7/2016 " + dr["t4"].ToString());
                                                        p44 = DateTime.Parse("4/7/2016 " + dr["t44"].ToString());
                                                        p5 = DateTime.Parse("4/7/2016 " + dr["t5"].ToString());
                                                        p55 = DateTime.Parse("4/7/2016 " + dr["t55"].ToString());
                                                        p6 = DateTime.Parse("4/7/2016 " + dr["t6"].ToString());
                                                        p66 = DateTime.Parse("4/7/2016 " + dr["t66"].ToString());
                                                        p7 = DateTime.Parse("4/7/2016 " + dr["t7"].ToString());
                                                        p77 = DateTime.Parse("4/7/2016 " + dr["t77"].ToString());
                                                        p8 = DateTime.Parse("4/7/2016 " + dr["t8"].ToString());
                                                        p88 = DateTime.Parse("4/7/2016 " + dr["t88"].ToString());
                                                        p9 = DateTime.Parse("4/7/2016 " + dr["t9"].ToString());
                                                        p99 = DateTime.Parse("4/7/2016 " + dr["t99"].ToString());
                                                        p10 = DateTime.Parse("4/7/2016 " + dr["t10"].ToString());
                                                        p100 = DateTime.Parse("4/7/2016 " + dr["t100"].ToString());
                                                        thr1 = (float)(dr["threshold1"]);
                                                        thr2 = (float)(dr["threshold2"]);
                                                        thr3 = (float)(dr["threshold3"]);
                                                        thr4 = (float)(dr["threshold4"]);
                                                        thr5 = (float)(dr["threshold5"]);
                                                        thr6 = (float)(dr["threshold6"]);
                                                        thr7 = (float)(dr["threshold7"]);
                                                        thr8 = (float)(dr["threshold8"]);
                                                        thr9 = (float)(dr["threshold9"]);
                                                        thr10 = (float)(dr["threshold10"]);
                                                        rthr = int.Parse(dr["rthreshold"].ToString());
                                                    }
                                                    dr.Close();
                                                }
                                                connection.Close();
                                            }

                                            DateTime Now = DateTime.Parse("4/7/2016 " + DateTime.Now.ToLongTimeString());

                                            if (planPresent)
                                            {
                                                if ((Now > p1 && Now < p11) || (Now > p2 && Now < p22) || (Now > p3 && Now < p33) || (Now > p4 && Now < p44) || (Now > p5 && Now < p55) || (Now > p6 && Now < p66) || (Now > p7 && Now < p77) || (Now > p8 && Now < p88) || (Now > p9 && Now < p99) || (Now > p10 && Now < p100))
                                                {
                                                    if (!planActive)
                                                    {
                                                        if (timerange(p1, p11)) { powerValue = thr1; }
                                                        if (timerange(p2, p22)) { powerValue = thr2; }
                                                        if (timerange(p3, p33)) { powerValue = thr3; }
                                                        if (timerange(p4, p44)) { powerValue = thr4; }
                                                        if (timerange(p5, p55)) { powerValue = thr5; }
                                                        if (timerange(p6, p66)) { powerValue = thr6; }
                                                        if (timerange(p7, p77)) { powerValue = thr7; }
                                                        if (timerange(p8, p88)) { powerValue = thr8; }
                                                        if (timerange(p9, p99)) { powerValue = thr9; }
                                                        if (timerange(p10, p100)) { powerValue = thr10; }

                                                        Console.WriteLine("Entering Plan: " + powerValue);

                                                        if (powerValue == 0)
                                                        {
                                                            stream.Write(RelayOff, 0, RelayOff.Length);
                                                            count = stream.Read(data, 0, data.Length);

                                                            meter_visuals.auxr_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                            stream.Write(relayOutputStatus, 0, relayOutputStatus.Length);
                                                            count = stream.Read(data, 0, data.Length);
                                                            if (data[8] == 14)
                                                            { /*DebugData(data, count)*/
                                                                ; throw new Exception("Data Error In LS Scheduling.");
                                                            }
                                                            relayStatus = data[count - 1];
                                                        }
                                                        else
                                                        {
                                                            string thres1 = Convert.ToInt32(powerValue).ToString("X8");
                                                            string command1 = "00 01 00 30 00 01 00 12 C1 01 C1 00 03 01 00 0F 23 00 00 02 00 06" + " " + thres1[0] + "" + thres1[1] + " " + thres1[2] + "" + thres1[3] + " " + thres1[4] + "" + thres1[5] + " " + thres1[6] + "" + thres1[7];
                                                            string command2 = "00 01 00 30 00 01 00 12 C1 01 C1 00 03 01 00 0F 23 00 01 02 00 06" + " " + thres1[0] + "" + thres1[1] + " " + thres1[2] + "" + thres1[3] + " " + thres1[4] + "" + thres1[5] + " " + thres1[6] + "" + thres1[7];
                                                            byte[] co1 = command1.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                            byte[] co2 = command2.Split().Select(s => Convert.ToByte(s, 16)).ToArray();

                                                            stream.Write(co1, 0, co1.Length);
                                                            count = stream.Read(data, 0, data.Length);
                                                            stream.Write(co2, 0, co2.Length);
                                                            count = stream.Read(data, 0, data.Length);

                                                            Console.WriteLine(DateTime.Now.ToString() + "Load Threshold Readjusted:" + MSN);
                                                        }

                                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                                        {
                                                            connection.Open();
                                                            using (OdbcCommand command = new OdbcCommand("UPDATE meter SET inloadshedding ='1' WHERE serial ='" + MSN + "';", connection))
                                                                command.ExecuteNonQuery();
                                                            connection.Close();
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (planActive)
                                                    {
                                                        int re = rthr;
                                                        string thres1 = re.ToString("X8");
                                                        string command1 = "00 01 00 30 00 01 00 12 C1 01 C1 00 03 01 00 0F 23 00 00 02 00 06" + " " + thres1[0] + "" + thres1[1] + " " + thres1[2] + "" + thres1[3] + " " + thres1[4] + "" + thres1[5] + " " + thres1[6] + "" + thres1[7];
                                                        string command2 = "00 01 00 30 00 01 00 12 C1 01 C1 00 03 01 00 0F 23 00 01 02 00 06" + " " + thres1[0] + "" + thres1[1] + " " + thres1[2] + "" + thres1[3] + " " + thres1[4] + "" + thres1[5] + " " + thres1[6] + "" + thres1[7];
                                                        byte[] co1 = command1.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        byte[] co2 = command2.Split().Select(s => Convert.ToByte(s, 16)).ToArray();

                                                        stream.Write(co1, 0, co1.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        stream.Write(co2, 0, co2.Length);
                                                        count = stream.Read(data, 0, data.Length);

                                                        stream.Write(RelayON, 0, RelayON.Length);
                                                        count = stream.Read(data, 0, data.Length);

                                                        meter_visuals.auxr_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                        stream.Write(relayOutputStatus, 0, relayOutputStatus.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        if (data[8] == 14)
                                                        { /*DebugData(data, count)*/
                                                            ; throw new Exception("Data Error in LS Implementation.");
                                                        }
                                                        relayStatus = data[count - 1];

                                                        Console.WriteLine("Resetting Plan");
                                                        using (OdbcConnection connection = new OdbcConnection(myconn))
                                                        {
                                                            connection.Open();
                                                            using (OdbcCommand command = new OdbcCommand("UPDATE meter SET inloadshedding ='0' WHERE serial ='" + MSN + "';", connection))
                                                                command.ExecuteNonQuery();
                                                            connection.Close();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        /**************************************Power Plan Adjustment*************************************************************/

                                        /******************************** Load Profile ************************************/
                                        if (loadprofileer == 1)
                                        {
                                            meter_visuals.last_command = "Load Profile Read";
                                            meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                            loadprofileer = LPCollect(stream, data, MSN, DateTime.Now, global_device_id);
                                            Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reading LP Data", DateTime.Now, 1);

                                            meter_visuals.last_command_resp = "Load Profile Read Successful";
                                            meter_visuals.last_command_resp_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                        }
                                        /*********************************************** Load Profile ************************************/

                                        /****************************************** Settings Read ********************************/
                                        if (flag)
                                        {

                                            //3-	IPPO (Programmed IP and Port)
                                            string gprsRead = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 19 04 80 FF 02 00";
                                            //Device Meta Data
                                            string gprsMode1Read = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 60 3C 06 FF 02 00";
                                            string firmwareVersionRead = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 60 01 02 FF 02 00";
                                            byte[] fvr = firmwareVersionRead.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                            stream.Write(fvr, 0, fvr.Length);
                                            count = stream.Read(data, 0, data.Length);
                                            string hex = BitConverter.ToString(data);
                                            hex = hex.Replace("-", string.Empty);
                                            string ascii = null;
                                            for (int i = 28; i < 94; i += 2)
                                            {
                                                String hs = string.Empty;
                                                hs = hex.Substring(i, 2);
                                                uint decval = System.Convert.ToUInt32(hs, 16);
                                                char character = System.Convert.ToChar(decval);
                                                ascii += character;
                                            }
                                            Console.WriteLine("Meter Number="+MSN+" Firmware Version = "+ascii);
                                            ExecuteNonQurey("update meter set firmwareVersion = '" + ascii + "' where serial = '" + MSN + "';");

                                            string RelayReconnectTimeRead = "00 01 00 30 00 01 00 0D C0 01 81 00 03 01 00 93 2C 00 FF 02 00";
                                            string RelayReconnectCountRead = "00 01 00 30 00 01 00 0D C0 01 81 00 01 01 00 93 2D 00 FF 02 00";
                                            //C1 01 81 00 03 01 00 0F 23 00 00 02 00 06
                                            //Point 1//
                                            string RelayOLTH1Read = "00 01 00 30 00 01 00 0D C0 01 81 00 03 01 00 0F 23 00 00 02 00";
                                            string RelayOLTH2Read = "00 01 00 30 00 01 00 0D C0 01 81 00 03 01 00 0F 23 00 01 02 00";
                                            string RelayOLTH1SensingTimeRead = "00 01 00 30 00 01 00 0D C0 01 81 00 03 01 00 0F 2C 00 00 02 00";
                                            string RelayOLTH2SensingTimeRead = "00 01 00 30 00 01 00 0D C0 01 81 00 03 01 00 0F 2C 00 01 02 00";
                                            string RelayRetryTH2 = "00 01 00 30 00 01 00 0D c0 01 81 00 03 01 00 93 2e 00 ff 02 00";

                                            string autoRDEsco = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 60 3C 0A FF 02 00";

                                            if (isSupposedToRead == 1)
                                            {
                                                bool isthere = false;
                                                using (OdbcConnection connection = new OdbcConnection(myconn))
                                                {
                                                    connection.Open();
                                                    using (OdbcCommand command = new OdbcCommand("SELECT * FROM settings WHERE serial = '" + MSN + "';", connection))
                                                    using (OdbcDataReader dr = command.ExecuteReader())
                                                    {
                                                        while (dr.Read())
                                                        {
                                                            isthere = true;
                                                        }
                                                        dr.Close();
                                                    }
                                                    connection.Close();
                                                }

                                                string apn = "";
                                                string ip1 = "";
                                                string ip2 = "";
                                                int port1 = 0;
                                                int port2 = 0;
                                                int keepAliveTime = 0;
                                                int gprsReconnectCount = 0;
                                                int gprsReconnectInterval = 0;
                                                int preprogramedIntervalBase = 0;
                                                int preprogramedInterval = 0;
                                                int mode = 0;
                                                string debug = "";

                                                switch (readType)
                                                {
                                                    case "customerSet":
                                                        Console.WriteLine(cu);
                                                        if (cu.Length < 13) { break; }
                                                        string cset = "00 01 00 30 00 01 00 1D C1 01 C1 00 01 00 00 60 01 0A FF 02 00 09 0E 3" + cu[0] + " 3" + cu[1] + " 3" + cu[2] + " 3" + cu[3] + " 3" + cu[4] + " 3" + cu[5] + " 3" + cu[6] + " 3" + cu[7] + " 3" + cu[8] + " 3" + cu[9] + " 3" + cu[10] + " 3" + cu[11] + " 3" + cu[12] + " 3" + cu[13];
                                                        byte[] bcset = cset.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        stream.Write(bcset, 0, bcset.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Setting Customer Code ", DateTime.Now, 1);
                                                        break;

                                                    case "customer":
                                                        byte[] customerC = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 193, 0, 1, 0, 0, 96, 1, 10, 255, 2, 0 };
                                                        stream.Write(customerC, 0, customerC.Length);
                                                        if (data[8] == 14)
                                                        {
                                                            //DebugData(data, count);
                                                            throw new Exception("Data Error in Customer Code Setting.");
                                                        }
                                                        count = stream.Read(data, 0, data.Length);
                                                        string ccode = (data[count - 14] - 48).ToString() + (data[count - 13] - 48).ToString() + (data[count - 12] - 48).ToString() + (data[count - 11] - 48).ToString() + (data[count - 10] - 48).ToString() + (data[count - 9] - 48).ToString() + (data[count - 8] - 48).ToString() + (data[count - 7] - 48).ToString() + (data[count - 6] - 48).ToString() + (data[count - 5] - 48).ToString() + (data[count - 4] - 48).ToString() + (data[count - 3] - 48).ToString() + (data[count - 2] - 48).ToString() + (data[count - 1] - 48).ToString();
                                                        ExecuteNonQurey("update meter set customerCode = '" + ccode + "' where serial = '" + MSN + "';");
                                                        Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reading Customer Code ", DateTime.Now, 1);
                                                        break;

                                                    case "gprs":
                                                        Console.WriteLine("here");
                                                        byte[] sendgprs = gprsRead.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        stream.Write(sendgprs, 0, sendgprs.Length);

                                                        for (int i = 0; i < sendgprs.Length; i++)
                                                        {
                                                            debug += sendgprs[i].ToString("X2") + " ";
                                                        }
                                                        Console.WriteLine(debug);
                                                        debug = "";
                                                        count = stream.Read(data, 0, data.Length);
                                                        for (int i = 0; i < count; i++)
                                                        {
                                                            debug += data[i].ToString("X2") + " ";
                                                        }
                                                        Console.WriteLine(debug);
                                                        debug = "";
                                                        mode = data[15];
                                                        ip1 = data[21].ToString("X2") + " " + data[22].ToString("X2") + " " + data[23].ToString("X2") + " " + data[24].ToString("X2");
                                                        ip2 = data[31].ToString("X2") + " " + data[32].ToString("X2") + " " + data[33].ToString("X2") + " " + data[34].ToString("X2");
                                                        port1 = (UInt16)((data[27] << 8) | (data[28] << 0));
                                                        port2 = (UInt16)((data[37] << 8) | (data[38] << 0));
                                                        keepAliveTime = data[count - 3];
                                                        gprsReconnectCount = data[count - 7];
                                                        gprsReconnectInterval = data[count - 5];
                                                        for (int i = 76; i < 76 + data[75]; i++)
                                                        {
                                                            apn += Convert.ToChar(data[i]);
                                                        }

                                                        byte[] sendgprsMode1 = gprsMode1Read.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        stream.Write(sendgprsMode1, 0, sendgprsMode1.Length);
                                                        for (int i = 0; i < sendgprsMode1.Length; i++)
                                                        {
                                                            debug += sendgprsMode1[i].ToString("X2") + " ";
                                                        }
                                                        Console.WriteLine(debug);
                                                        debug = "";
                                                        count = stream.Read(data, 0, data.Length);
                                                        for (int i = 0; i < count; i++)
                                                        {
                                                            debug += data[i].ToString("X2") + " ";
                                                        }
                                                        Console.WriteLine(debug);
                                                        debug = "";
                                                        preprogramedInterval = data[count - 1];
                                                        preprogramedIntervalBase = data[count - 3];
                                                        if (isthere)
                                                        {
                                                            Console.WriteLine("Was here");
                                                            ExecuteNonQurey("UPDATE settings SET IP1 = '" + ip1 + "', IP2 = '" + ip2 + "', port1 = '" + port1 + "', port2 = '" + port2 + "', keepAlive='" + keepAliveTime + "', gprsReconnectCount='" + gprsReconnectCount + "', gprsReconnectTime = '" + preprogramedIntervalBase + "', gprsReconnectInterval = '" + preprogramedInterval + "', mode = '" + mode + "', apn='" + apn + "' WHERE serial ='" + MSN + "';");
                                                        }
                                                        else
                                                        {
                                                            ExecuteNonQurey("insert into settings(serial,relayReconnectCount,relayReconnectTime,overloadThreshold1,overloadThreshold2,reconnectInterval,cycleReset,enabled,IP1,IP2,port1,port2,apn,keepAlive,gprsReconnectCount,gprsReconnectTime,gprsReconnectInterval,mode) values('" + MSN + "','5','5','23000','23000','23000','60','1','" + ip1 + "','" + ip2 + "','" + port1 + "','" + port2 + "','" + apn + "','" + keepAliveTime + "','" + gprsReconnectCount + "','" + preprogramedIntervalBase + "','" + preprogramedInterval + "','" + mode + "');");
                                                        }
                                                        break;

                                                    case "sim":
                                                        string sm1 = "";
                                                        string sm2 = "";
                                                        string sm3 = "";
                                                        string sim1Read = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 60 0C 80 FF 02 00";
                                                        string sim2Read = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 60 0C 81 FF 02 00";
                                                        string sim3Read = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 60 0C 82 FF 02 00";
                                                        byte[] sendSim1 = sim1Read.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        byte[] sendSim2 = sim2Read.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        byte[] sendSim3 = sim3Read.Split().Select(s => Convert.ToByte(s, 16)).ToArray();

                                                        stream.Write(sendSim1, 0, sendSim1.Length);
                                                        stream.Read(data, 0, data.Length);
                                                        for (int i = 14; i < 14 + data[13]; i++)
                                                        {
                                                            sm1 += Convert.ToChar(data[i]);
                                                        }

                                                        stream.Write(sendSim2, 0, sendSim2.Length);
                                                        stream.Read(data, 0, data.Length);
                                                        for (int i = 14; i < 14 + data[13]; i++)
                                                        {
                                                            sm2 += Convert.ToChar(data[i]);
                                                        }

                                                        stream.Write(sendSim3, 0, sendSim3.Length);
                                                        stream.Read(data, 0, data.Length);
                                                        for (int i = 14; i < 14 + data[13]; i++)
                                                        {
                                                            sm3 += Convert.ToChar(data[i]);
                                                        }
                                                        if (isthere)
                                                        {
                                                            Console.WriteLine("Was here");
                                                            ExecuteNonQurey("UPDATE settings SET sim1 = '" + sm1 + "', sim2 = '" + sm2 + "', sim3 = '" + sm3 + "' WHERE serial ='" + MSN + "';");
                                                        }
                                                        else
                                                        {
                                                            ExecuteNonQurey("insert into settings(serial,relayReconnectCount,relayReconnectTime,overloadThreshold1,overloadThreshold2,reconnectInterval,cycleReset,enabled,sim1,sim2,sim3) values('" + MSN + "','5','5','23000','23000','23000','60','1','" + sm1 + "','" + sm2 + "','" + sm3 + "');");
                                                        }
                                                        Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Setting SIM Numbers", DateTime.Now, 1);
                                                        break;

                                                    case "relay":
                                                        int rrecontime = 0;
                                                        int rreconcount = 0;
                                                        UInt32 rolth1 = 0;
                                                        UInt32 rolth2 = 0;
                                                        int rolthst1 = 0;
                                                        int rolthst2 = 0;
                                                        int retCycReset = 0;
                                                        int enabled = 1;

                                                        byte[] relayReconTime = RelayReconnectTimeRead.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        byte[] relayReconCount = RelayReconnectCountRead.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        byte[] relayOLth1 = RelayOLTH1Read.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        byte[] relayOlth2 = RelayOLTH2Read.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        byte[] relayOlth1SensingTime = RelayOLTH1SensingTimeRead.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        byte[] relayOlth2SensingTime = RelayOLTH2SensingTimeRead.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        byte[] relayRetryCycleReset = RelayRetryTH2.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        byte[] relautodesR = autoRDEsco.Split().Select(s => Convert.ToByte(s, 16)).ToArray();

                                                        stream.Write(relayReconTime, 0, relayReconTime.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        if (data[8] == 14) { throw new Exception("Data Read Error"); }
                                                        rrecontime = (UInt16)((data[count - 2] << 8) | (data[count - 1] << 0));
                                                        stream.Write(relayReconCount, 0, relayReconCount.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        if (data[8] == 14) { throw new Exception("Data Read Error"); }
                                                        rreconcount = data[count - 1];
                                                        stream.Write(relayOLth1, 0, relayOLth1.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        if (data[8] == 14) { throw new Exception("Data Read Error"); }
                                                        rolth1 = (UInt32)((data[count - 4] << 24) | (data[count - 3] << 16) | (data[count - 2] << 8) | (data[count - 1] << 0));
                                                        stream.Write(relayOlth2, 0, relayOlth2.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        if (data[8] == 14) { throw new Exception("Data Read Error"); }
                                                        rolth2 = (UInt32)((data[count - 4] << 24) | (data[count - 3] << 16) | (data[count - 2] << 8) | (data[count - 1] << 0));
                                                        stream.Write(relayOlth1SensingTime, 0, relayOlth1SensingTime.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        if (data[8] == 14) { throw new Exception("Data Read Error"); }
                                                        rolthst1 = (UInt16)((data[count - 2] << 8) | (data[count - 1] << 0));
                                                        stream.Write(relayOlth2SensingTime, 0, relayOlth2SensingTime.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        if (data[8] == 14) { throw new Exception("Data Read Error"); }
                                                        rolthst2 = (UInt16)((data[count - 2] << 8) | (data[count - 1] << 0));
                                                        stream.Write(relayRetryCycleReset, 0, relayRetryCycleReset.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        if (data[8] == 14) { throw new Exception("Data Read Error"); }
                                                        retCycReset = data[count - 1];
                                                        stream.Write(relayOutputStatus, 0, relayOutputStatus.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        if (data[8] == 14) { throw new Exception("Data Read Error"); }
                                                        relayStatus = data[count - 1];
                                                        stream.Write(relautodesR, 0, relautodesR.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        if (data[8] == 14) { throw new Exception("Data Read Error"); }
                                                        enabled = data[count - 1];
                                                        meter_visuals.sanc_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                        if (isthere)
                                                        {
                                                            ExecuteNonQurey("UPDATE settings SET enabled = '" + enabled + "', relayReconnectCount = '" + rreconcount + "', relayReconnectTime = '" + rolthst2 + "', overloadThreshold1 = '" + rolth1 + "', overloadThreshold2 = '" + rolth2 + "', reconnectInterval = '" + rrecontime + "', cycleReset = '" + retCycReset + "'  WHERE serial ='" + MSN + "';");
                                                        }
                                                        else
                                                        {
                                                            ExecuteNonQurey("insert into settings(serial,relayReconnectCount,relayReconnectTime,overloadThreshold1,overloadThreshold2,reconnectInterval,cycleReset,enabled) values('" + MSN + "','" + rreconcount + "','" + rolthst1 + "','" + rolth1 + "','" + rolth2 + "','" + rrecontime + "','" + retCycReset + "','" + enabled + "');");
                                                        }
                                                        ExecuteNonQurey("replace into meter.relay_configuration values('" + MSN + "', '" + global_device_id + "', '" + rreconcount + "', '" + rolthst1 + "', '" + rolth1 + "', '" + rolth2 + "', '" + rolthst1 + "','" + retCycReset + "', '" + enabled + "');");
                                                        Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Setting Relay Params", DateTime.Now, 1);
                                                        break;

                                                    case "events":
                                                        event_read = read_event_data(stream, data, MSN, global_device_id);
                                                        Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reading Events", DateTime.Now, 1);
                                                        break;

                                                    default:
                                                        break;
                                                }
                                            }
                                        }
                                        /*****************************************************************************************/

                                        if (flag)
                                        {
                                            /******************************************Configuration*****************************/
                                            string[] ConfigCommands = new string[20];

                                            if (configure == 1)
                                            {
                                                bool ball = false;
                                                configure = 0;
                                                using (OdbcConnection connection = new OdbcConnection(myconn))
                                                {
                                                    connection.Open();
                                                    using (OdbcCommand command = new OdbcCommand("SELECT command FROM configuration WHERE serial = '" + MSN + "';", connection))
                                                    using (OdbcDataReader dr = command.ExecuteReader())
                                                    {
                                                        while (dr.Read())
                                                        {
                                                            ConfigCommands[configure] = (string)(dr["command"]);
                                                            if (dr["command"].ToString().Contains("00 01 00 30 00 01 00 10 C1 01 C1 00 03 01 00 93 2C 00 FF 02 00 12")) { ball = true; }
                                                            configure++;
                                                        }
                                                        dr.Close();
                                                    }
                                                    connection.Close();
                                                }

                                                if (configure > 0)
                                                {
                                                    for (int o = 0; o < configure; o++)
                                                    {
                                                        byte[] configcomm = ConfigCommands[o].Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                        string tu = "";
                                                        for (int r = 0; r < configcomm.Length; r++)
                                                        {
                                                            tu += configcomm[r].ToString("X2") + " ";
                                                        }
                                                        Console.WriteLine(tu);
                                                        stream.Write(configcomm, 0, configcomm.Length);
                                                        int roch = stream.Read(data, 0, data.Length);

                                                        tu = "";
                                                        for (int r = 0; r < roch; r++)
                                                        {
                                                            tu += data[r].ToString("X2") + " ";
                                                        }
                                                        Console.WriteLine(tu);
                                                    }

                                                    if (ball)
                                                    {
                                                        stream.Write(RelayON, 0, RelayON.Length);
                                                        count = stream.Read(data, 0, data.Length);
                                                        meter_visuals.auxr_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                    }

                                                    string readModeSettings = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 19 04 80 FF 02 00";
                                                    byte[] readModes = readModeSettings.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                    stream.Write(readModes, 0, readModes.Length);
                                                    stream.Read(data, 0, data.Length);
                                                    if (data[15] == 0)
                                                    {
                                                        ExecuteNonQurey("UPDATE meter SET mode ='1' WHERE serial ='" + MSN + "';");
                                                    }
                                                    else if (data[15] == 1)
                                                    {
                                                        ExecuteNonQurey("UPDATE meter SET mode ='0' WHERE serial ='" + MSN + "';");
                                                    }

                                                    isitMode2 = data[15] == 1;

                                                    Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reading Configuration Data ", DateTime.Now, 1);
                                                    ExecuteNonQurey("delete from configuration1 where serial ='" + maulTheConfigCommand + "';");
                                                }
                                            }
                                            /*************************************************************************************/

                                            if (/*relayStatus != 0 && */relayDCRequest == 1)
                                            {
                                                ExecuteNonQurey("update transaction_status1 set status_level='4', status_4_datetime = '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "' where transactionid = '" + transactionid + "' and msn='" + MSN + "';");

                                                meter_visuals.last_command = "Relay Disconnection Request";
                                                meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                stream.Write(RelayOff, 0, RelayOff.Length);
                                                count = stream.Read(data, 0, data.Length);
                                              ExecuteNonQurey("UPDATE meter SET relayDCRequest = 0 where `serial` = " + MSN + ";");
                                            
                                            Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Disconnecting Relay", DateTime.Now, 1);
                                                meter_visuals.auxr_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                                meter_visuals.last_command_resp = "Relay Disconnected";
                                                meter_visuals.last_command_resp_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                stream.Write(relayOutputStatus, 0, relayOutputStatus.Length);
                                                count = stream.Read(data, 0, data.Length);
                                                if (data[8] == 14)
                                                {
                                                    //DebugData(data, count);
                                                    throw new Exception("Data Error in Contactor DC Request.");
                                                }
                                                relayStatus = data[count - 1];
                                                Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Checking Relay Status", DateTime.Now, 1);
                                            ExecuteNonQurey("update meter.transaction_status1 set completed = 1,status_level='5', status_5_datetime = '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "' where msn='" + MSN + "' and transactionid = '" + transactionid + "';");
                                            }

                                            if (/*relayStatus == 0 && */relayRecon == 1)
                                            {
                                                ExecuteNonQurey("update transaction_status1 set status_level='4', status_4_datetime = '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "' where transactionid = '" + transactionid + "' and msn='" + MSN + "';");

                                                meter_visuals.last_command = "Relay Reconnection Request";
                                                meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                stream.Write(RelayON, 0, RelayON.Length);
                                                count = stream.Read(data, 0, data.Length);
                                            ExecuteNonQurey("UPDATE meter SET relayReconnect=0,relayStatus=1 where `serial` = " + MSN + ";");
                                            Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reconnecting Relay", DateTime.Now, 1);
                                                meter_visuals.auxr_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                meter_visuals.last_command_resp = "Relay Reconnected";
                                                meter_visuals.last_command_resp_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                stream.Write(relayOutputStatus, 0, relayOutputStatus.Length);
                                                count = stream.Read(data, 0, data.Length);
                                                if (data[8] == 14)
                                                {
                                                    //DebugData(data, count);
                                                    throw new Exception("Data Error in Contactor Recon Request.");
                                                }
                                                relayStatus = data[count - 1];
                                                Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Checking Relay Status", DateTime.Now, 1);

                                                ExecuteNonQurey("update meter.transaction_status1 set completed = 1,status_level='5', status_5_datetime = '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "' where msn='" + MSN + "' and transactionid = '" + transactionid + "';");
                                            }

                                            /////////////////////Instantanous Data/////////////////////////////////////////////
                                            if (instantaaa == 1)
                                            {
                                                meter_visuals.last_command = "Read Instantaneous Data";
                                                meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                //instantaaa = instantaneous_Collecter(stream, data, MSN, DateTime.Now, global_device_id);
                                                Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reading Instanteous Data", DateTime.Now, 1);

                                                meter_visuals.last_command_resp = "Instantaneous Data Successful";
                                                meter_visuals.last_command_resp_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");
                                            }
                                            /////////////////////Billing Data/////////////////////////////////////////////


                                            if (daily_bill_read == 1)
                                            {
                                                meter_visuals.last_command = "Read Billing Data";
                                                meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                daily_bill_read = read_billing_interval_1(stream, MSN, data, global_device_id, ref last_active_energy_pos_tl, ref last_active_energy_pos_tl_datetime, ref last_active_energy_neg_tl, ref last_active_energy_neg_tl_datetime, ref last_reactive_energy_pos_tl, ref last_reactive_energy_pos_tl_datetime, ref last_reactive_energy_neg_tl, ref last_reactive_energy_neg_tl_datetime);
                                                Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reading Daily Billing Interval 1", DateTime.Now, 1);
                                                Console.WriteLine(MSN + ": Read Complete Dailly Billing Int 1");

                                                daily_bill_read = Daily_Billing(stream, MSN, data, global_device_id, ref last_active_energy_pos_tl, ref last_active_energy_pos_tl_datetime, ref last_active_energy_neg_tl, ref last_active_energy_neg_tl_datetime, ref last_reactive_energy_pos_tl, ref last_reactive_energy_pos_tl_datetime, ref last_reactive_energy_neg_tl, ref last_reactive_energy_neg_tl_datetime);
                                                Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reading Daily Billing Interval 2", DateTime.Now, 1);
                                            }
                                            //Reading Events

                                            if (event_read == 1)
                                            {
                                                event_read = read_event_data(stream, data, MSN, global_device_id);
                                                Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Reading Events", DateTime.Now, 1);
                                            }
                                            /*******************************************************************/
                                            /**********************************************************************/
                                            ExecuteNonQurey("UPDATE meter SET lastRead ='" + String.Format("{0:yyyy/M/d HH:mm:ss}", DateTime.Now) + "' WHERE msn ='" + n + "';");

                                            /////////////////////////Read Portion////////////////////////////////////
                                            if (t == 1)
                                            {
                                                Console.WriteLine("Reading Current Billing " + MSN);
                                                t = read_billing_data(stream, data, MSN, global_device_id);
                                                Console.WriteLine("Complete Current Billing " + MSN);
                                            }

                                            if (monthly_bill_read == 1)
                                            {
                                                Console.WriteLine("Reading Monthly Billing " + MSN);
                                                monthly_bill_read = read_monthly_billing_data(stream, data, MSN, global_device_id);
                                                Console.WriteLine("Complete Monthly Billing " + MSN);
                                            }
                                            //////////////////////////On Demand Read////////////////////////////////

                                            if (t == 1 || instantaaa == 1 || syncer == 1)
                                            {
                                                double KWh = 0;
                                                byte[] yyyy = BitConverter.GetBytes(DateTime.Now.Year);
                                                byte[] kwhOBIS = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 15, 8, 0, 255, 2, 0 };
                                                byte[] kwhOBISTarrif1 = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 15, 8, 1, 255, 2, 0 };
                                                byte[] kwhOBISTariff2 = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 15, 8, 2, 255, 2, 0 };
                                                byte[] KVar_q1q3 = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 94, 92, 0, 255, 2, 0 };
                                                byte[] kVAR_q1q3tariff1 = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 94, 92, 1, 255, 2, 0 };
                                                byte[] kVAR_q1q3tariff2 = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 94, 92, 2, 255, 2, 0 };
                                                byte[] MDITL = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 4, 1, 0, 15, 6, 0, 255, 2, 0 };
                                                byte[] MDITLtarrif1 = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 4, 1, 0, 15, 6, 1, 255, 2, 0 };
                                                byte[] MDITLtariff2 = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 4, 1, 0, 15, 6, 2, 255, 2, 0 };
                                                byte[] CumMDITL = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 4, 1, 0, 15, 2, 0, 255, 2, 0 };
                                                byte[] CumMDIariff1 = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 4, 1, 0, 15, 2, 1, 255, 2, 0 };
                                                byte[] CumMDIIariff2 = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 4, 1, 0, 15, 2, 2, 255, 2, 0 };
                                                byte[] AggActivePowerImp = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 1, 7, 0, 255, 2, 0 };
                                                byte[] AggActivePowerExp = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 2, 7, 0, 255, 2, 0 };
                                                byte[] AggReactivePowerImp = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 3, 7, 0, 255, 2, 0 };
                                                byte[] AggReactivePowerExp = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 4, 7, 0, 255, 2, 0 };
                                                byte[] CurrentPhaseA = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 31, 7, 0, 255, 2, 0 };
                                                byte[] CurrentPhaseB = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 51, 7, 0, 255, 2, 0 };
                                                byte[] CurrentPhaseC = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 71, 7, 0, 255, 2, 0 };
                                                byte[] VoltagePhaseA = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 32, 7, 0, 255, 2, 0 };
                                                byte[] VoltagePhaseB = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 52, 7, 0, 255, 2, 0 };
                                                byte[] VoltagePhaseC = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 72, 7, 0, 255, 2, 0 };
                                                byte[] PF = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 13, 7, 0, 255, 2, 0 };
                                                byte[] Frequency = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 3, 1, 0, 14, 7, 0, 255, 2, 0 };
                                                byte[] Time = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 1, 1, 0, 0, 9, 1, 255, 2, 0 };
                                                byte[] Date = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 1, 1, 0, 0, 9, 2, 255, 2, 0 };
                                                byte[] currTariff = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 1, 0, 0, 96, 14, 0, 255, 2, 0 };
                                                byte[] MDIResetDTGet = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 1, 1, 0, 0, 9, 7, 255, 2, 0 };
                                                byte[] MDIRessetCount = { 0, 1, 0, 48, 0, 1, 0, 13, 192, 1, 129, 0, 1, 1, 0, 0, 1, 0, 255, 2, 0 };
                                                //Console.WriteLine("Will Start Parsing parsing");

                                                //Console.WriteLine("Step 2");
                                                byte[] next_block = "00 01 00 30 00 01 00 07 C0 02 81 00 00 00 01".Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                                                //Console.WriteLine("Step 3");

                                                if (syncer == 1)
                                                {
                                                    string timeSync = "00 01 00 30 00 01 00 1B C1 01 81 00 08 00 00 01 00 00 FF 02 00 09 0C " + yyyy[1].ToString("X2") + " " + yyyy[0].ToString("X2") + " " + DateTime.Now.Month.ToString("X2") + " " + DateTime.Now.Day.ToString("X2") + " 03 " + DateTime.Now.Hour.ToString("X2") + " " + DateTime.Now.Minute.ToString("X2") + " " + DateTime.Now.Second.ToString("X2") + " 00 80 00 00";

                                                    byte[] timesyncbytes = timeSync.Split().Select(s => Convert.ToByte(s, 16)).ToArray();

                                                    meter_visuals.last_command = "Time Sync";
                                                    meter_visuals.last_command_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                    stream.Write(timesyncbytes, 0, timesyncbytes.Length);
                                                    count = stream.Read(data, 0, data.Length);
                                                    Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Datetime Sync", DateTime.Now, 1);
                                                    meter_visuals.last_command_resp = "Time Sync Successful";
                                                    meter_visuals.last_command_resp_datetime = DateTime.Now.ToString("yyyy/M/d HH:mm:ss");

                                                    if (data[count - 1] == 0)
                                                    {
                                                        ExecuteNonQurey("UPDATE meter SET sync ='0' WHERE msn ='" + n + "';");
                                                    }
                                                    this.BeginInvoke(new Action(() =>
                                                    {

                                                        tbReactivePower.Text = "Synced";
                                                    }));

                                                }

                                                ///////////////////////////////updation end////////////////


                                                /***************** PITC Data*************/

                                                Console.Write("Meter Serial Number" + checker + "\n");
                                                //if (checker == MSN.ToString())
                                                //{
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET current_tariff_register ='" + currentTarrrr + "' WHERE `msn` ='" + MSN + "';");


                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET signal_strength ='" + SignalStrength + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET meter_datetime ='" + meterDateTimeInst + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET  current_phase_a ='" + CurrentA + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET  current_phase_b ='" + CurrentB + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET  current_phase_c ='" + CurrentC + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET  voltage_phase_a ='" + VoltageA + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET  voltage_phase_b ='" + VoltageB + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET  voltage_phase_c ='" + VoltageC + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET  aggregate_active_pwr_pos ='" + AggActivePowerImport + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET  aggregate_active_pwr_neg ='" + AggActivePowerExport + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET  aggregate_reactive_pwr_pos ='" + AggRactivePowerImport + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE `random_instantaneous_data` SET  aggregate_reactive_pwr_neg ='" + AggReactivePowerExport + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE random_instantaneous_data SET   average_pf ='" + PowerFactor + "' WHERE `msn` ='" + MSN + "';");
                                                //    ExecuteNonQurey("UPDATE random_instantaneous_data SET   mdc_read_datetime ='" + meterDateTimeInst + "' WHERE `msn` ='" + MSN + "';");
                                                //}
                                                //else
                                                //{

                                                /************************************************************************/
                                            }
                                        }
                                    

                                    if (aarqStatus)
                                    {
                                        byte[] cosemrelease = { 0, 1, 0, 48, 0, 1, 0, 5, 98, 3, 128, 1, 0 };
                                        stream.Write(cosemrelease, 0, cosemrelease.Length);
                                        count = stream.Read(data, 0, data.Length);
                                        //DebugData(data, count);
                                        Log_Comm_Sequence(MSN, global_device_id, longitude, latitude, "Releasing Handshake", DateTime.Now, 1);
                                        aarqStatus = false;

                                        if (!isitMode2)
                                        {
                                            PushEventsCheck(stream, remoteEndPoint, isitMode2, ref aarqStatus);
                                        }
                                    }
                                    this.BeginInvoke(new Action(() => {

                                        label5.Text = MSN + ": Handshake Released at:" + DateTime.Now.TimeOfDay.ToString();
                                    }));
                                    Console.WriteLine(MSN + ": Handshake Released at:" + DateTime.Now.TimeOfDay.ToString());

                                    if (isitMode2)
                                    {
                                        //stream.ReadTimeout = 1800000;
                                    }
                                    else
                                    {
                                        stream.Close();
                                        tcpClient.Close();
                                        Console.WriteLine(MSN + " :Session Ended - MODE I Device :" + DateTime.Now.ToString());
                                        return;
                                    }
                                    Console.WriteLine(MSN + " :Session Ended- MODE II Device :" + DateTime.Now.ToString());

                                    faliures = 0;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);

                            ExecuteNonQurey("UPDATE meter.meter SET reading ='0' WHERE serial ='" + MSN + "';");

                            if (isitMode2)
                            {
                                stream.ReadTimeout = 1800000;
                            }
                            else
                            {
                                stream.Close();
                                tcpClient.Close();
                                isitMode2 = false;
                            }
                            faliures++;
                            if (faliures > 8)
                            {
                                stream.Close();
                                tcpClient.Close();
                                isitMode2 = false;
                            }
                        }
                    }
                    while (isitMode2);
                    Console.WriteLine(MSN + ": Disconnected @ " + DateTime.Now.ToString());
                }

            }
        }
        private dynamic PushEventsCheck(NetworkStream stream, string remoteEndPoint, bool mode2, ref bool aarq)
        {
            uint MSN = 0;
            byte[] data = new byte[1024];
            byte[] eventAck = { 0, 1, 0, 16, 0, 1, 0, 1, 219 };
            int count = 0;
            bool isEvent = false;

            try
            {
                Console.WriteLine(DateTime.Now.ToString() + " Listening From Client IP: " + remoteEndPoint);
                if (mode2)
                {
                    stream.ReadTimeout = 1800000;
                }
                else
                {
                    stream.ReadTimeout = 5000;
                }

                do
                {
                    isEvent = false;
                    count = stream.Read(data, 0, data.Length);
                    string str2 = "";
                    for (int index = 0; index < count; ++index)
                    {
                        str2 = str2 + data[index].ToString("X2") + " ";
                    }

                    if (data[count - 1] == 0x07 && data[count - 2] == 0x00 && data[count - 3] == 0x2C)
                    {
                        aarq = true;
                    }

                    if (data[1] == 4)
                    {
                        MSN = (UInt32)((data[count - 1] << 24) | (data[count - 2] << 16) | (data[count - 3] << 8) | (data[count - 4] << 0));
                        byte[] keepAliveAck = { 218 };
                        Console.WriteLine(DateTime.Now.ToString() + "  Keep Alive Receieved From:  " + str2 + " Meter Number: " + MSN);
                        string my_str = this.convertr(keepAliveAck);
                        stream.Write(keepAliveAck, 0, keepAliveAck.Length);
                        Console.WriteLine("Sent:" + DateTime.Now.ToLongTimeString() + " Keep Alive Ack sent to meter:" + MSN + " Packet Sended:" + my_str);

                    }

                    if (data[count - 1] == 0 && data[count - 2] == 9)
                    {
                        isEvent = true;
                        Console.WriteLine(DateTime.Now.ToString() + "  Event Receieved From:  " + remoteEndPoint + "\t" + str2 + "\t Meter Number: " + MSN);
                        stream.Write(eventAck, 0, eventAck.Length);
                        string my_str = this.convertr(eventAck);

                        UInt16 eventCode = (UInt16)((data[count - 4] << 8) | (data[count - 3] << 0));
                        string Serial = (data[count - 28] - 48).ToString() + (data[count - 27] - 48).ToString() + (data[count - 26] - 48).ToString() + (data[count - 25] - 48).ToString() + (data[count - 24] - 48).ToString() + (data[count - 23] - 48).ToString() + (data[count - 22] - 48).ToString() + (data[count - 21] - 48).ToString() + (data[count - 20] - 48).ToString() + (data[count - 19] - 48).ToString();
                        string occurTime = data[count - 12] + ":" + data[count - 11] + ":" + data[count - 10];
                        UInt16 dateoccur = (UInt16)((data[count - 17] << 8) | (data[count - 16] << 0));
                        string occurance = dateoccur + "/" + data[count - 15] + "/" + data[count - 14] + " " + occurTime;
                        MSN = UInt32.Parse(Serial);
                        string global_device_id = "";
                        Console.WriteLine(DateTime.Now.ToString() + " Event Ack send:" + MSN + "\t Packet Sended:" + my_str);

                        DataTable dt = ExecuteReader("SELECT global_device_id FROM meter.meter WHERE `serial` = '" + MSN + "';");
                        foreach (DataRow dr in dt.Rows)
                        {
                            global_device_id = dr["global_device_id"].ToString();
                        }
                        if (eventCode != 0)
                        {
                            ExecuteNonQurey("INSERT IGNORE INTO meter.events1 VALUES('" + MSN + "', '" + global_device_id + "', '" + occurance + "', '" + eventCode + "', '" + eventCodeTranslation(eventCode.ToString()) + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");
                        }
                    }
                }
                while (isEvent);
            }
            catch (Exception) { }

            Console.WriteLine(DateTime.Now.ToString() + " End Listening: " + remoteEndPoint);
            return MSN;
        }
        private int initial_device_configuration(NetworkStream stream, uint MSN, string global_device_id)
        {
            DataTable dt = ExecuteReader("SELECT * FROM meter.device WHERE dsn = '" + MSN + "';");
            int communication_interval = 30;
            int communication_type = 0;
            int mdi_reset_date = 1;
            string mdi_reset_time = "00:00:00";
            int count = 0;
            byte[] data = new byte[1024];
            //byte[] gprsRead = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 19 04 80 FF 02 00".Split().Select(s => Convert.ToByte(s, 16)).ToArray();

            foreach (DataRow dr in dt.Rows)
            {
                communication_interval = int.Parse(dr["communication_interval"].ToString());
                communication_type = int.Parse(dr["communication_type"].ToString());
                mdi_reset_date = int.Parse(dr["mdi_reset_date"].ToString());
                mdi_reset_time = dr["mdi_reset_time"].ToString();
            }

            DateTime dateTime = DateTime.Parse("2000/1/1 " + mdi_reset_time);

            //********************** Read Current GPRS Parameters ************************//
            /*stream.Write(gprsRead, 0, gprsRead.Length);
            count = stream.Read(data, 0, data.Length);
            if (data[8] == 14) { throw new Exception(MSN + ": Device Configuration Error!"); }

            string ip1 = data[21].ToString("X2") + " " + data[22].ToString("X2") + " " + data[23].ToString("X2") + " " + data[24].ToString("X2");
            string ip2 = data[31].ToString("X2") + " " + data[32].ToString("X2") + " " + data[33].ToString("X2") + " " + data[34].ToString("X2");
            string port1 = data[27].ToString("X2") + " " + data[28].ToString("X2");
            string port2 = data[37].ToString("X2") + " " + data[38].ToString("X2");*/

            /*string IP = IPBox.Text;
            string[] IPs = IP.Split('.');
            string port = serverPort.Text;
            int portPrim = int.Parse(port);
            int portSeco = int.Parse(port);

            string p1 = portPrim.ToString("X4");
            string p2 = portSeco.ToString("X4");

            string p11 = p1[0] + "" + p1[1] + " " + p1[2] + "" + p1[3];
            string p22 = p2[0] + "" + p2[1] + " " + p2[2] + "" + p2[3];*/

            //string mode = communication_type == 1 ? "01" : "00";
            // C1 01 81 00 16 00 00 0F 00 00 FF 04 00 01 02 02 04 09 04 0F 12 00 FF 09 05 FF FF FF 12 FF
            string mdi_reset_command = "00 01 00 30 00 01 00 11 C1 01 81 00 01 01 00 00 08 06 FF 02 00 09 02 " + mdi_reset_date.ToString("X2") + " " + dateTime.Hour.ToString("X2");
            //string program_mode_Ip_settings = "00 01 00 30 00 01 00 5A C1 02 81 00 01 00 00 19 04 80 FF 02 00 00 00 00 00 01 47 02 15 11 " + communication_type.ToString("X2") + " 04 01 02 09 04 " + ip1 + " 09 02 " + port1 + " 09 04 " + ip2 + " 09 02 " + port2 + " 09 04 FF FF FF FF 09 02 FF FF 09 04 FF FF FF FF 09 02 FF FF 0A 01 30 09 04 FF FF FF FF 09 04 FF FF FF FF 0A 04 61 75 74 6F 0A 01 30";
            //string program_Mode2_settings = "00 01 00 30 00 01 00 68 C1 01 81 00 01 00 00 19 04 80 FF 02 00 02 15 11 " + communication_type.ToString("X2") + " 04 01 02 09 04 " + ip1 + " 09 02 " + port1 + " 09 04 " + ip2 + " 09 02 " + port2 + " 09 04 FF FF FF FF 09 02 FF FF 09 04 FF FF FF FF 09 02 FF FF 0A 01 30 09 04 FF FF FF FF 09 04 FF FF FF FF 0A 04 61 75 74 6F 0A 01 30 0A 04 6E 75 6C 6C 0A 04 6E 75 6C 6C 11 03 11 01 11 05 11 00";
            //string Mode_2_params = "C1 03 81 FF 00 00 00 02 14 0A 04 6E 75 6C 6C 0A 04 6E 75 6C 6C 11 0A 11 0A 11 0A 11 00";
            //string mode_1_params = "00 01 00 30 00 01 00 13 C1 01 81 00 01 00 00 60 3C 06 FF 02 00 02 02 11 00 11 " + (communication_interval > 59 ? "3B" : communication_interval.ToString("X2"));

            byte[] mdi_command = mdi_reset_command.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
            //byte[] program_mode = program_Mode2_settings.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
            //byte[] mode_1_program = mode_1_params.Split().Select(s => Convert.ToByte(s, 16)).ToArray();

            stream.Write(mdi_command, 0, mdi_command.Length);
            count = stream.Read(data, 0, data.Length);
            if (data[8] == 14) { throw new Exception(MSN + ": Device Configuration Error!"); }

            /*stream.Write(program_mode, 0, program_mode.Length);
            count = stream.Read(data, 0, data.Length);
            if (data[8] == 14) { throw new Exception(MSN + ": Device Configuration Error!"); }

            stream.Write(mode_1_program, 0, mode_1_program.Length);
            count = stream.Read(data, 0, data.Length);
            if (data[8] == 14) { throw new Exception(MSN + ": Device Configuration Error!"); }*/

            //int comm_mode = communication_type == 1 ? 0 : 1;
            //ExecuteNonQurey("update meter.meter set `mode` = '" + comm_mode + "' where `serial` = '" + MSN + "';");

            return 1;
        }
        private int configure_relay(NetworkStream stream, uint MSN, byte[] data)
        {
            List<string> _byte_strings = new List<string>();
            SanctionedLoadStruct sanctionedLoadStruct = new SanctionedLoadStruct();
            DataTable data_T = ExecuteReader("SELECT * FROM meter.relay_configuration where msn = '" + MSN + "';");
            foreach (DataRow row in data_T.Rows)
            {
                sanctionedLoadStruct.msn = (string)row["msn"];
                sanctionedLoadStruct.global_device_id = (string)row["global_device_id"];
                sanctionedLoadStruct.relay_reconnect_count = UInt64.Parse(row["relay_reconnect_count"].ToString());
                sanctionedLoadStruct.relay_reconnect_inteval = UInt64.Parse(row["relay_reconnect_inteval"].ToString());
                sanctionedLoadStruct.overload_threshold_1 = UInt64.Parse(row["overload_threshold_1"].ToString());
                sanctionedLoadStruct.overload_threshold_2 = UInt64.Parse(row["overload_threshold_2"].ToString());
                sanctionedLoadStruct.cycle_reset = UInt64.Parse(row["cycle_reset"].ToString());
                sanctionedLoadStruct.threshold_duration = UInt64.Parse(row["threshold_duration"].ToString());
                sanctionedLoadStruct.enabled = int.Parse(row["enabled"].ToString());

                string retryCycleRest = "00 01 00 30 00 01 00 0F C1 01 81 00 03 01 00 93 2E 00 FF 02 00 11 " + (sanctionedLoadStruct.cycle_reset > 255 ? 255 : sanctionedLoadStruct.cycle_reset).ToString("X2");
                _byte_strings.Add(retryCycleRest);

                string thres1 = (sanctionedLoadStruct.overload_threshold_1 > 99999 ? 90000 : sanctionedLoadStruct.overload_threshold_1).ToString("X8");
                string thres2 = (sanctionedLoadStruct.overload_threshold_2 > 99999 ? 90000 : sanctionedLoadStruct.overload_threshold_2).ToString("X8");
                byte[] reconnecttimethresh = BitConverter.GetBytes(sanctionedLoadStruct.threshold_duration > 255 ? 255 : sanctionedLoadStruct.threshold_duration);
                byte[] reconnectinterval = BitConverter.GetBytes(sanctionedLoadStruct.relay_reconnect_inteval > 255 ? 255 : sanctionedLoadStruct.relay_reconnect_inteval);
                byte rI1 = 0, rI2 = 0;
                if (reconnectinterval.Length < 2)
                {
                    rI2 = reconnectinterval[0];
                    rI1 = 0;
                }
                else
                {
                    rI1 = reconnectinterval[0];
                    rI2 = reconnectinterval[1];
                }

                string sendreconnectInterval = "00 01 00 30 00 01 00 10 C1 01 C1 00 03 01 00 93 2C 00 FF 02 00 12 " + rI2.ToString("X2") + " " + rI1.ToString("X2");
                string commander = "00 01 00 30 00 01 00 12 C1 01 C1 00 03 01 00 0F 23 00 00 02 00 06" + " " + thres1[0] + "" + thres1[1] + " " + thres1[2] + "" + thres1[3] + " " + thres1[4] + "" + thres1[5] + " " + thres1[6] + "" + thres1[7];
                string commander1 = "00 01 00 30 00 01 00 12 C1 01 C1 00 03 01 00 0F 23 00 01 02 00 06" + " " + thres2[0] + "" + thres2[1] + " " + thres2[2] + "" + thres2[3] + " " + thres2[4] + "" + thres2[5] + " " + thres2[6] + "" + thres2[7];
                string reconnectcount = "00 01 00 30 00 01 00 0F C1 01 C1 00 01 01 00 93 2D 00 FF 02 00 11 " + (sanctionedLoadStruct.relay_reconnect_count > 255 ? 255 : sanctionedLoadStruct.relay_reconnect_count).ToString("X2");
                _byte_strings.Add(sendreconnectInterval);
                _byte_strings.Add(commander);
                _byte_strings.Add(commander1);
                _byte_strings.Add(reconnectcount);

                string reconnecttimethreshold = "";
                string reconnectimtethresh1 = "";
                if (reconnecttimethresh.Length == 1)
                {
                    reconnecttimethreshold = "00 01 00 30 00 01 00 10 c1 01 81 00 03 01 00 0f 2c 00 00 02 00 12 00 " + reconnecttimethresh[0].ToString("X2");
                    reconnectimtethresh1 = "00 01 00 30 00 01 00 10 c1 01 81 00 03 01 00 0f 2c 00 01 02 00 12 00 " + reconnecttimethresh[0].ToString("X2");
                }
                else if (reconnecttimethresh.Length > 1)
                {
                    reconnecttimethreshold = "00 01 00 30 00 01 00 10 c1 01 81 00 03 01 00 0f 2c 00 00 02 00 12 " + reconnecttimethresh[1].ToString("X2") + " " + reconnecttimethresh[0].ToString("X2");
                    reconnectimtethresh1 = "00 01 00 30 00 01 00 10 c1 01 81 00 03 01 00 0f 2c 00 01 02 00 12 " + reconnecttimethresh[1].ToString("X2") + " " + reconnecttimethresh[0].ToString("X2");
                }
                _byte_strings.Add(reconnecttimethreshold);
                _byte_strings.Add(reconnectimtethresh1);

                foreach (var config_comm in _byte_strings)
                {
                    //Console.WriteLine(config_comm);
                    byte[] config_command = config_comm.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
                    int count = 0;
                    stream.Write(config_command, 0, config_command.Length);
                    count = stream.Read(data, 0, data.Length);
                    if (data[8] == 14)
                    { throw new Exception("Data Error: Relay Config"); }
                    //Console.WriteLine(data[count-1].ToString("X2"));
                }
            }
            return 1;
        }
        private int check_relay_status(NetworkStream stream, uint MSN, byte[] data)
        {
            int count = 0;
            byte[] relayOutputStatus = "00 01 00 30 00 01 00 0D C0 01 C1 00 46 00 00 60 03 0A FF 02 00".Split().Select(s => Convert.ToByte(s, 16)).ToArray();
            stream.Write(relayOutputStatus, 0, relayOutputStatus.Length);
            count = stream.Read(data, 0, data.Length);
            if (data[8] == 14)
            { /*DebugData(data, count)*/; throw new Exception("Data Error: Relay Status Check."); }
            return data[count - 1];
        }
        private int Daily_Billing(NetworkStream stream, uint MSN, byte[] data, string global_device_id, ref double last_active_energy_pos_tl, ref string last_active_energy_pos_tl_datetime, ref double last_active_energy_neg_tl, ref string last_active_energy_neg_tl_datetime, ref double last_reactive_energy_pos_tl, ref string last_reactive_energy_pos_tl_datetime, ref double last_reactive_energy_neg_tl, ref string last_reactive_energy_neg_tl_datetime)
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine(MSN + ": Daily Billing Reading Started");

            int count = 0;
            Array.Clear(data, 0, data.Length);
            DateTime tha_date = DateTime.Now.AddDays(-2);
            byte[] yyyy = BitConverter.GetBytes(tha_date.Year);
            string daily_billing_interval_2 = "00 01 00 30 00 01 00 40 C0 01 81 00 07 01 00 63 02 00 FF 02 01 01 02 04 02 04 12 00 08 09 06 00 00 01 00 00 FF 0F 02 12 00 00 09 0C " + yyyy[1].ToString("X2") + " " + yyyy[0].ToString("X2") + " " + tha_date.Month.ToString("X2") + " " + tha_date.Day.ToString("X2") + " 04 " + DateTime.Now.Hour.ToString("X2") + " " + DateTime.Now.Minute.ToString("X2") + " " + DateTime.Now.Second.ToString("X2") + " 00 80 00 00 09 0C " + yyyy[1].ToString("X2") + " " + yyyy[0].ToString("X2") + " " + DateTime.Now.Month.ToString("X2") + " " + DateTime.Now.Day.ToString("X2") + " 04 " + DateTime.Now.Hour.ToString("X2") + " " + DateTime.Now.Minute.ToString("X2") + " " + DateTime.Now.Second.ToString("X2") + " 00 80 00 00 01 00";

            byte[] daily_billing_interval_2_hex = daily_billing_interval_2.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
            byte[] next_block = "00 01 00 30 00 01 00 07 C0 02 81 00 00 00 01".Split().Select(s => Convert.ToByte(s, 16)).ToArray();
            stream.Write(daily_billing_interval_2_hex, 0, daily_billing_interval_2_hex.Length);
            count = stream.Read(data, 0, data.Length);
           
            if (data[8] == 14) { return 1; }

            //if (count < 274) { return 0; }
            if (data[7] != 6)
            {
                List<byte> blk_1 = new List<byte>();
                for (int i = 0; i < count; i++)
                {
                    blk_1.Add(data[i]);
                }

                int index_b1 = count;
                
                UInt16 year_b1 = (UInt16)((blk_1[index_b1 - 274] << 8) | (blk_1[index_b1 - 273] << 0));
                string date_time_b1 = year_b1 + "/" + blk_1[index_b1 - 272] + "/" + blk_1[index_b1 - 271] + " " + blk_1[index_b1 - 269] + ":" + blk_1[index_b1 - 268] + ":" + blk_1[index_b1 - 267];
                double load_profile_daily = ((UInt32)((blk_1[index_b1 - 261] << 24) | (blk_1[index_b1 - 260] << 16) | (blk_1[index_b1 - 259] << 8) | (blk_1[index_b1 - 258] << 0)));
                double active_energy_pos_t1 = ((UInt32)((blk_1[index_b1 - 256] << 24) | (blk_1[index_b1 - 255] << 16) | (blk_1[index_b1 - 254] << 8) | (blk_1[index_b1 - 253] << 0))) * 0.001;
                double active_energy_pos_t2 = ((UInt32)((blk_1[index_b1 - 251] << 24) | (blk_1[index_b1 - 250] << 16) | (blk_1[index_b1 - 249] << 8) | (blk_1[index_b1 - 248] << 0))) * 0.001;
                double active_energy_pos_tl = ((UInt32)((blk_1[index_b1 - 246] << 24) | (blk_1[index_b1 - 245] << 16) | (blk_1[index_b1 - 244] << 8) | (blk_1[index_b1 - 243] << 0))) * 0.001;
                double active_energy_neg_t1 = ((UInt32)((blk_1[index_b1 - 241] << 24) | (blk_1[index_b1 - 240] << 16) | (blk_1[index_b1 - 239] << 8) | (blk_1[index_b1 - 238] << 0))) * 0.001;
                double active_energy_neg_t2 = ((UInt32)((blk_1[index_b1 - 236] << 24) | (blk_1[index_b1 - 235] << 16) | (blk_1[index_b1 - 234] << 8) | (blk_1[index_b1 - 233] << 0))) * 0.001;
                double active_energy_neg_tl = ((UInt32)((blk_1[index_b1 - 231] << 24) | (blk_1[index_b1 - 230] << 16) | (blk_1[index_b1 - 229] << 8) | (blk_1[index_b1 - 228] << 0))) * 0.001;
                double active_energy_abs_t1 = ((UInt32)((blk_1[index_b1 - 226] << 24) | (blk_1[index_b1 - 225] << 16) | (blk_1[index_b1 - 224] << 8) | (blk_1[index_b1 - 223] << 0))) * 0.001;
                double active_energy_abs_t2 = ((UInt32)((blk_1[index_b1 - 221] << 24) | (blk_1[index_b1 - 220] << 16) | (blk_1[index_b1 - 219] << 8) | (blk_1[index_b1 - 218] << 0))) * 0.001;
                double active_energy_abs_tl = ((UInt32)((blk_1[index_b1 - 216] << 24) | (blk_1[index_b1 - 215] << 16) | (blk_1[index_b1 - 214] << 8) | (blk_1[index_b1 - 213] << 0))) * 0.001;
                double reactive_energy_pos_t1 = ((UInt32)((blk_1[index_b1 - 211] << 24) | (blk_1[index_b1 - 210] << 16) | (blk_1[index_b1 - 209] << 8) | (blk_1[index_b1 - 208] << 0))) * 0.001;
                double reactive_energy_pos_t2 = ((UInt32)((blk_1[index_b1 - 206] << 24) | (blk_1[index_b1 - 205] << 16) | (blk_1[index_b1 - 204] << 8) | (blk_1[index_b1 - 203] << 0))) * 0.001;
                double reactive_energy_pos_tl = ((UInt32)((blk_1[index_b1 - 201] << 24) | (blk_1[index_b1 - 200] << 16) | (blk_1[index_b1 - 199] << 8) | (blk_1[index_b1 - 198] << 0))) * 0.001;
                double reactive_energy_neg_t1 = ((UInt32)((blk_1[index_b1 - 196] << 24) | (blk_1[index_b1 - 195] << 16) | (blk_1[index_b1 - 194] << 8) | (blk_1[index_b1 - 193] << 0))) * 0.001;
                double reactive_energy_neg_t2 = ((UInt32)((blk_1[index_b1 - 191] << 24) | (blk_1[index_b1 - 190] << 16) | (blk_1[index_b1 - 189] << 8) | (blk_1[index_b1 - 188] << 0))) * 0.001;
                double reactive_energy_neg_tl = ((UInt32)((blk_1[index_b1 - 186] << 24) | (blk_1[index_b1 - 185] << 16) | (blk_1[index_b1 - 184] << 8) | (blk_1[index_b1 - 183] << 0))) * 0.001;
                double reactive_energy_abs_t1 = ((UInt32)((blk_1[index_b1 - 181] << 24) | (blk_1[index_b1 - 180] << 16) | (blk_1[index_b1 - 179] << 8) | (blk_1[index_b1 - 178] << 0))) * 0.001;
                double reactive_energy_abs_t2 = ((UInt32)((blk_1[index_b1 - 176] << 24) | (blk_1[index_b1 - 175] << 16) | (blk_1[index_b1 - 174] << 8) | (blk_1[index_b1 - 173] << 0))) * 0.001;
                double reactive_energy_abs_tl = ((UInt32)((blk_1[index_b1 - 171] << 24) | (blk_1[index_b1 - 170] << 16) | (blk_1[index_b1 - 169] << 8) | (blk_1[index_b1 - 168] << 0))) * 0.001;
                double active_mdi_pos_t1 = ((UInt32)((blk_1[index_b1 - 166] << 24) | (blk_1[index_b1 - 165] << 16) | (blk_1[index_b1 - 164] << 8) | (blk_1[index_b1 - 163] << 0))) * 0.001;
                double active_mdi_pos_t2 = ((UInt32)((blk_1[index_b1 - 161] << 24) | (blk_1[index_b1 - 160] << 16) | (blk_1[index_b1 - 159] << 8) | (blk_1[index_b1 - 158] << 0))) * 0.001;
                double active_mdi_pos_tl = ((UInt32)((blk_1[index_b1 - 156] << 24) | (blk_1[index_b1 - 155] << 16) | (blk_1[index_b1 - 154] << 8) | (blk_1[index_b1 - 153] << 0))) * 0.001;
                double active_mdi_neg_t1 = ((UInt32)((blk_1[index_b1 - 151] << 24) | (blk_1[index_b1 - 150] << 16) | (blk_1[index_b1 - 149] << 8) | (blk_1[index_b1 - 148] << 0))) * 0.001;
                double active_mdi_neg_t2 = ((UInt32)((blk_1[index_b1 - 146] << 24) | (blk_1[index_b1 - 145] << 16) | (blk_1[index_b1 - 144] << 8) | (blk_1[index_b1 - 143] << 0))) * 0.001;
                double active_mdi_neg_tl = ((UInt32)((blk_1[index_b1 - 141] << 24) | (blk_1[index_b1 - 140] << 16) | (blk_1[index_b1 - 139] << 8) | (blk_1[index_b1 - 138] << 0))) * 0.001;
                double active_mdi_abs_t1 = ((UInt32)((blk_1[index_b1 - 136] << 24) | (blk_1[index_b1 - 135] << 16) | (blk_1[index_b1 - 134] << 8) | (blk_1[index_b1 - 133] << 0))) * 0.001;
                double active_mdi_abs_t2 = ((UInt32)((blk_1[index_b1 - 131] << 24) | (blk_1[index_b1 - 130] << 16) | (blk_1[index_b1 - 129] << 8) | (blk_1[index_b1 - 128] << 0))) * 0.001;
                double active_mdi_abs_tl = ((UInt32)((blk_1[index_b1 - 126] << 24) | (blk_1[index_b1 - 125] << 16) | (blk_1[index_b1 - 124] << 8) | (blk_1[index_b1 - 123] << 0))) * 0.001;
                double cumulative_mdi_abs_t1 = ((UInt32)((blk_1[index_b1 - 121] << 24) | (blk_1[index_b1 - 120] << 16) | (blk_1[index_b1 - 119] << 8) | (blk_1[index_b1 - 118] << 0))) * 0.001;
                double cumulative_mdi_abs_t2 = ((UInt32)((blk_1[index_b1 - 116] << 24) | (blk_1[index_b1 - 115] << 16) | (blk_1[index_b1 - 114] << 8) | (blk_1[index_b1 - 113] << 0))) * 0.001;
                double cumulative_mdi_abs_tl = ((UInt32)((blk_1[index_b1 - 111] << 24) | (blk_1[index_b1 - 110] << 16) | (blk_1[index_b1 - 109] << 8) | (blk_1[index_b1 - 108] << 0))) * 0.001;
                double cumulative_mdi_pos_t1 = ((UInt32)((blk_1[index_b1 - 106] << 24) | (blk_1[index_b1 - 105] << 16) | (blk_1[index_b1 - 104] << 8) | (blk_1[index_b1 - 103] << 0))) * 0.001;
                double cumulative_mdi_pos_t2 = ((UInt32)((blk_1[index_b1 - 101] << 24) | (blk_1[index_b1 - 100] << 16) | (blk_1[index_b1 - 99] << 8) | (blk_1[index_b1 - 98] << 0))) * 0.001;
                double cumulative_mdi_pos_tl = ((UInt32)((blk_1[index_b1 - 96] << 24) | (blk_1[index_b1 - 95] << 16) | (blk_1[index_b1 - 94] << 8) | (blk_1[index_b1 - 93] << 0))) * 0.001;
                double cumulative_mdi_neg_t1 = ((UInt32)((blk_1[index_b1 - 91] << 24) | (blk_1[index_b1 - 90] << 16) | (blk_1[index_b1 - 89] << 8) | (blk_1[index_b1 - 88] << 0))) * 0.001;
                double cumulative_mdi_neg_t2 = ((UInt32)((blk_1[index_b1 - 86] << 24) | (blk_1[index_b1 - 85] << 16) | (blk_1[index_b1 - 84] << 8) | (blk_1[index_b1 - 83] << 0))) * 0.001;
                double cumulative_mdi_neg_tl = ((UInt32)((blk_1[index_b1 - 81] << 24) | (blk_1[index_b1 - 80] << 16) | (blk_1[index_b1 - 79] << 8) | (blk_1[index_b1 - 78] << 0))) * 0.001;
                
                last_active_energy_pos_tl = active_energy_pos_tl;
                last_active_energy_pos_tl_datetime = date_time_b1;
                last_active_energy_neg_tl = active_energy_neg_tl;
                last_active_energy_neg_tl_datetime = date_time_b1;
                last_reactive_energy_pos_tl = reactive_energy_pos_tl;
                last_reactive_energy_pos_tl_datetime = date_time_b1;
                last_reactive_energy_neg_tl = reactive_energy_neg_tl;
                last_reactive_energy_neg_tl_datetime = date_time_b1;
                //DB Write
                ExecuteNonQurey("INSERT INTO meter.daily_billing VALUES('" + MSN + "', '" + global_device_id + "', '" + date_time_b1 + "', '" + load_profile_daily + "', '" + active_energy_pos_t1 + "', '" + active_energy_pos_t2 + "', '" + active_energy_pos_tl + "', '" + active_energy_neg_t1 + "', '" + active_energy_neg_t2 + "', '" + active_energy_neg_tl + "', '" + active_energy_abs_t1 + "', '" + active_energy_abs_t2 + "', '" + active_energy_abs_tl + "', '" + reactive_energy_pos_t1 + "', '" + reactive_energy_pos_t2 + "', '" + reactive_energy_pos_tl + "', '" + reactive_energy_neg_t1 + "', '" + reactive_energy_neg_t2 + "', '" + reactive_energy_neg_tl + "', '" + reactive_energy_abs_t1 + "', '" + reactive_energy_abs_t2 + "', '" + reactive_energy_abs_tl + "', '" + active_mdi_pos_t1 + "', '" + active_mdi_pos_t2 + "', '" + active_mdi_pos_tl + "', '" + active_mdi_neg_t1 + "', '" + active_mdi_neg_t2 + "', '" + active_mdi_neg_tl + "', '" + active_mdi_abs_t1 + "', '" + active_mdi_abs_t2 + "', '" + active_mdi_abs_tl + "', '" + cumulative_mdi_abs_t1 + "', '" + cumulative_mdi_abs_t2 + "', '" + cumulative_mdi_abs_tl + "', '" + cumulative_mdi_pos_t1 + "', '" + cumulative_mdi_pos_t2 + "', '" + cumulative_mdi_pos_tl + "', '" + cumulative_mdi_neg_t1 + "', '" + cumulative_mdi_neg_t2 + "', '" + cumulative_mdi_neg_tl + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");
                Console.WriteLine(MSN + " :Daily Billing Read Complete");
            }
            return 0;
        }
        
        private int read_billing_interval_1(NetworkStream stream, uint MSN, byte[] data, string global_device_id, ref double last_active_energy_pos_tl, ref string last_active_energy_pos_tl_datetime, ref double last_active_energy_neg_tl, ref string last_active_energy_neg_tl_datetime, ref double last_reactive_energy_pos_tl, ref string last_reactive_energy_pos_tl_datetime, ref double last_reactive_energy_neg_tl, ref string last_reactive_energy_neg_tl_datetime)
        {
            byte[] yyyy = BitConverter.GetBytes(DateTime.Now.Year);
            string daily_billing_interval_1 = "00 01 00 30 00 01 00 40 C0 01 81 00 07 01 00 62 03 00 FF 02 01 01 02 04 02 04 12 00 07 09 06 00 00 01 00 00 FF 0F 02 12 00 00 09 0C " + yyyy[1].ToString("X2") + " " + yyyy[0].ToString("X2") + " " + DateTime.Now.Month.ToString("X2") + " " + DateTime.Now.Day.ToString("X2") + " 01 08 3B 00 00 80 00 00 09 0C " + yyyy[1].ToString("X2") + " " + yyyy[0].ToString("X2") + " " + DateTime.Now.Month.ToString("X2") + " " + DateTime.Now.Day.ToString("X2") + " 01 09 01 00 00 80 00 00 01 00";
            byte[] daily_billing_interval_1_hex = daily_billing_interval_1.Split().Select(s => Convert.ToByte(s, 16)).ToArray();
            byte[] next_block = "00 01 00 30 00 01 00 07 C0 02 81 00 00 00 01".Split().Select(s => Convert.ToByte(s, 16)).ToArray();

            int count = 0;
            stream.Write(daily_billing_interval_1_hex, 0, daily_billing_interval_1_hex.Length);
            count = stream.Read(data, 0, data.Length);
            //Console.WriteLine("Step 1");

            if (data[8] == 14) { return 1; }
            if (count < 274)
            {
                Console.WriteLine("UDIL Parameters Incomplete");
                return 0;
            }
            if (data[7] != 6)
            {
                List<byte> blk_1 = new List<byte>();
                for (int i = 0; i < count; i++)
                {
                    blk_1.Add(data[i]);
                }

                int index_b1 = count;
                //Console.WriteLine("Step 2");
                stream.Write(next_block, 0, next_block.Length);
                count = stream.Read(data, 0, data.Length);

                UInt16 year_b1 = (UInt16)((blk_1[index_b1 - 274] << 8) | (blk_1[index_b1 - 273] << 0));
                string date_time_b1 = year_b1 + "/" + blk_1[index_b1 - 272] + "/" + blk_1[index_b1 - 271] + " " + blk_1[index_b1 - 269] + ":" + blk_1[index_b1 - 268] + ":" + blk_1[index_b1 - 267];

                double active_energy_pos_t1 = ((UInt32)((blk_1[index_b1 - 261] << 24) | (blk_1[index_b1 - 260] << 16) | (blk_1[index_b1 - 259] << 8) | (blk_1[index_b1 - 258] << 0))) * 0.001;
                double active_energy_pos_t2 = ((UInt32)((blk_1[index_b1 - 256] << 24) | (blk_1[index_b1 - 255] << 16) | (blk_1[index_b1 - 254] << 8) | (blk_1[index_b1 - 253] << 0))) * 0.001;
                double active_energy_pos_t3 = ((UInt32)((blk_1[index_b1 - 251] << 24) | (blk_1[index_b1 - 250] << 16) | (blk_1[index_b1 - 249] << 8) | (blk_1[index_b1 - 248] << 0))) * 0.001;
                double active_energy_pos_t4 = ((UInt32)((blk_1[index_b1 - 246] << 24) | (blk_1[index_b1 - 245] << 16) | (blk_1[index_b1 - 244] << 8) | (blk_1[index_b1 - 243] << 0))) * 0.001;
                double active_energy_pos_tl = ((UInt32)((blk_1[index_b1 - 241] << 24) | (blk_1[index_b1 - 240] << 16) | (blk_1[index_b1 - 239] << 8) | (blk_1[index_b1 - 238] << 0))) * 0.001;

                double active_energy_neg_t1 = ((UInt32)((blk_1[index_b1 - 236] << 24) | (blk_1[index_b1 - 235] << 16) | (blk_1[index_b1 - 234] << 8) | (blk_1[index_b1 - 233] << 0))) * 0.001;
                double active_energy_neg_t2 = ((UInt32)((blk_1[index_b1 - 231] << 24) | (blk_1[index_b1 - 230] << 16) | (blk_1[index_b1 - 229] << 8) | (blk_1[index_b1 - 228] << 0))) * 0.001;
                double active_energy_neg_t3 = ((UInt32)((blk_1[index_b1 - 226] << 24) | (blk_1[index_b1 - 225] << 16) | (blk_1[index_b1 - 224] << 8) | (blk_1[index_b1 - 223] << 0))) * 0.001;
                double active_energy_neg_t4 = ((UInt32)((blk_1[index_b1 - 221] << 24) | (blk_1[index_b1 - 220] << 16) | (blk_1[index_b1 - 219] << 8) | (blk_1[index_b1 - 218] << 0))) * 0.001;
                double active_energy_neg_tl = ((UInt32)((blk_1[index_b1 - 216] << 24) | (blk_1[index_b1 - 215] << 16) | (blk_1[index_b1 - 214] << 8) | (blk_1[index_b1 - 213] << 0))) * 0.001;

                double active_energy_abs_t1 = ((UInt32)((blk_1[index_b1 - 211] << 24) | (blk_1[index_b1 - 210] << 16) | (blk_1[index_b1 - 209] << 8) | (blk_1[index_b1 - 208] << 0))) * 0.001;
                double active_energy_abs_t2 = ((UInt32)((blk_1[index_b1 - 206] << 24) | (blk_1[index_b1 - 205] << 16) | (blk_1[index_b1 - 204] << 8) | (blk_1[index_b1 - 203] << 0))) * 0.001;
                double active_energy_abs_t3 = ((UInt32)((blk_1[index_b1 - 201] << 24) | (blk_1[index_b1 - 200] << 16) | (blk_1[index_b1 - 199] << 8) | (blk_1[index_b1 - 198] << 0))) * 0.001;
                double active_energy_abs_t4 = ((UInt32)((blk_1[index_b1 - 196] << 24) | (blk_1[index_b1 - 195] << 16) | (blk_1[index_b1 - 194] << 8) | (blk_1[index_b1 - 193] << 0))) * 0.001;
                double active_energy_abs_tl = ((UInt32)((blk_1[index_b1 - 191] << 24) | (blk_1[index_b1 - 190] << 16) | (blk_1[index_b1 - 189] << 8) | (blk_1[index_b1 - 188] << 0))) * 0.001;

                double reactive_energy_pos_t1 = ((UInt32)((blk_1[index_b1 - 186] << 24) | (blk_1[index_b1 - 185] << 16) | (blk_1[index_b1 - 184] << 8) | (blk_1[index_b1 - 183] << 0))) * 0.001;
                double reactive_energy_pos_t2 = ((UInt32)((blk_1[index_b1 - 181] << 24) | (blk_1[index_b1 - 180] << 16) | (blk_1[index_b1 - 179] << 8) | (blk_1[index_b1 - 178] << 0))) * 0.001;
                double reactive_energy_pos_t3 = ((UInt32)((blk_1[index_b1 - 176] << 24) | (blk_1[index_b1 - 175] << 16) | (blk_1[index_b1 - 174] << 8) | (blk_1[index_b1 - 173] << 0))) * 0.001;
                double reactive_energy_pos_t4 = ((UInt32)((blk_1[index_b1 - 171] << 24) | (blk_1[index_b1 - 170] << 16) | (blk_1[index_b1 - 169] << 8) | (blk_1[index_b1 - 168] << 0))) * 0.001;
                double reactive_energy_pos_tl = ((UInt32)((blk_1[index_b1 - 166] << 24) | (blk_1[index_b1 - 165] << 16) | (blk_1[index_b1 - 164] << 8) | (blk_1[index_b1 - 163] << 0))) * 0.001;

                double reactive_energy_neg_t1 = ((UInt32)((blk_1[index_b1 - 161] << 24) | (blk_1[index_b1 - 160] << 16) | (blk_1[index_b1 - 159] << 8) | (blk_1[index_b1 - 158] << 0))) * 0.001;
                double reactive_energy_neg_t2 = ((UInt32)((blk_1[index_b1 - 156] << 24) | (blk_1[index_b1 - 155] << 16) | (blk_1[index_b1 - 154] << 8) | (blk_1[index_b1 - 153] << 0))) * 0.001;
                double reactive_energy_neg_t3 = ((UInt32)((blk_1[index_b1 - 151] << 24) | (blk_1[index_b1 - 150] << 16) | (blk_1[index_b1 - 149] << 8) | (blk_1[index_b1 - 148] << 0))) * 0.001;
                double reactive_energy_neg_t4 = ((UInt32)((blk_1[index_b1 - 146] << 24) | (blk_1[index_b1 - 145] << 16) | (blk_1[index_b1 - 144] << 8) | (blk_1[index_b1 - 143] << 0))) * 0.001;
                double reactive_energy_neg_tl = ((UInt32)((blk_1[index_b1 - 141] << 24) | (blk_1[index_b1 - 140] << 16) | (blk_1[index_b1 - 139] << 8) | (blk_1[index_b1 - 138] << 0))) * 0.001;

                double reactive_energy_abs_t1 = ((UInt32)((blk_1[index_b1 - 136] << 24) | (blk_1[index_b1 - 135] << 16) | (blk_1[index_b1 - 134] << 8) | (blk_1[index_b1 - 133] << 0))) * 0.001;
                double reactive_energy_abs_t2 = ((UInt32)((blk_1[index_b1 - 131] << 24) | (blk_1[index_b1 - 130] << 16) | (blk_1[index_b1 - 129] << 8) | (blk_1[index_b1 - 128] << 0))) * 0.001;
                double reactive_energy_abs_t3 = ((UInt32)((blk_1[index_b1 - 126] << 24) | (blk_1[index_b1 - 125] << 16) | (blk_1[index_b1 - 124] << 8) | (blk_1[index_b1 - 123] << 0))) * 0.001;
                double reactive_energy_abs_t4 = ((UInt32)((blk_1[index_b1 - 121] << 24) | (blk_1[index_b1 - 120] << 16) | (blk_1[index_b1 - 119] << 8) | (blk_1[index_b1 - 118] << 0))) * 0.001;
                double reactive_energy_abs_tl = ((UInt32)((blk_1[index_b1 - 116] << 24) | (blk_1[index_b1 - 115] << 16) | (blk_1[index_b1 - 114] << 8) | (blk_1[index_b1 - 113] << 0))) * 0.001;

                double active_mdi_pos_t1 = ((UInt32)((blk_1[index_b1 - 86] << 24) | (blk_1[index_b1 - 85] << 16) | (blk_1[index_b1 - 84] << 8) | (blk_1[index_b1 - 83] << 0))) * 0.001;
                double active_mdi_pos_t2 = ((UInt32)((blk_1[index_b1 - 67] << 24) | (blk_1[index_b1 - 66] << 16) | (blk_1[index_b1 - 65] << 8) | (blk_1[index_b1 - 64] << 0))) * 0.001;
                double active_mdi_pos_t3 = ((UInt32)((blk_1[index_b1 - 48] << 24) | (blk_1[index_b1 - 47] << 16) | (blk_1[index_b1 - 46] << 8) | (blk_1[index_b1 - 45] << 0))) * 0.001;
                double active_mdi_pos_t4 = ((UInt32)((blk_1[index_b1 - 29] << 24) | (blk_1[index_b1 - 28] << 16) | (blk_1[index_b1 - 27] << 8) | (blk_1[index_b1 - 26] << 0))) * 0.001;
                double active_mdi_pos_tl = ((UInt32)((blk_1[index_b1 - 10] << 24) | (blk_1[index_b1 - 9] << 16) | (blk_1[index_b1 - 8] << 8) | (blk_1[index_b1 - 7] << 0))) * 0.001;

                double active_mdi_neg_t1 = ((UInt32)((blk_1[count - 264] << 24) | (blk_1[count - 263] << 16) | (blk_1[count - 262] << 8) | (blk_1[count - 261] << 0))) * 0.001;
                double active_mdi_neg_t2 = ((UInt32)((blk_1[count - 245] << 24) | (blk_1[count - 244] << 16) | (blk_1[count - 243] << 8) | (blk_1[count - 242] << 0))) * 0.001;
                double active_mdi_neg_t3 = ((UInt32)((blk_1[count - 226] << 24) | (blk_1[count - 225] << 16) | (blk_1[count - 224] << 8) | (blk_1[count - 223] << 0))) * 0.001;
                double active_mdi_neg_t4 = ((UInt32)((blk_1[count - 207] << 24) | (blk_1[count - 206] << 16) | (blk_1[count - 205] << 8) | (blk_1[count - 204] << 0))) * 0.001;
                double active_mdi_neg_tl = ((UInt32)((blk_1[count - 188] << 24) | (blk_1[count - 187] << 16) | (blk_1[count - 186] << 8) | (blk_1[count - 185] << 0))) * 0.001;

                double active_mdi_abs_t1 = ((UInt32)((blk_1[count - 169] << 24) | (blk_1[count - 168] << 16) | (blk_1[count - 167] << 8) | (blk_1[count - 166] << 0))) * 0.001;
                double active_mdi_abs_t2 = ((UInt32)((blk_1[count - 150] << 24) | (blk_1[count - 149] << 16) | (blk_1[count - 148] << 8) | (blk_1[count - 147] << 0))) * 0.001;
                double active_mdi_abs_t3 = ((UInt32)((blk_1[count - 131] << 24) | (blk_1[count - 130] << 16) | (blk_1[count - 129] << 8) | (blk_1[count - 128] << 0))) * 0.001;
                double active_mdi_abs_t4 = ((UInt32)((blk_1[count - 112] << 24) | (blk_1[count - 111] << 16) | (blk_1[count - 110] << 8) | (blk_1[count - 109] << 0))) * 0.001;
                double active_mdi_abs_tl = ((UInt32)((blk_1[count - 93] << 24) | (blk_1[count - 92] << 16) | (blk_1[count - 91] << 8) | (blk_1[count - 90] << 0))) * 0.001;

                double cumulative_mdi_pos_t1 = ((UInt32)((blk_1[count - 74] << 24) | (blk_1[count - 73] << 16) | (blk_1[count - 72] << 8) | (blk_1[count - 71] << 0))) * 0.001;
                double cumulative_mdi_pos_t2 = ((UInt32)((blk_1[count - 69] << 24) | (blk_1[count - 68] << 16) | (blk_1[count - 67] << 8) | (blk_1[count - 66] << 0))) * 0.001;
                double cumulative_mdi_pos_t3 = ((UInt32)((blk_1[count - 64] << 24) | (blk_1[count - 63] << 16) | (blk_1[count - 62] << 8) | (blk_1[count - 61] << 0))) * 0.001;
                double cumulative_mdi_pos_t4 = ((UInt32)((blk_1[count - 59] << 24) | (blk_1[count - 58] << 16) | (blk_1[count - 57] << 8) | (blk_1[count - 56] << 0))) * 0.001;
                double cumulative_mdi_pos_tl = ((UInt32)((blk_1[count - 54] << 24) | (blk_1[count - 53] << 16) | (blk_1[count - 52] << 8) | (blk_1[count - 51] << 0))) * 0.001;

                double cumulative_mdi_neg_t1 = ((UInt32)((blk_1[count - 49] << 24) | (blk_1[count - 48] << 16) | (blk_1[count - 47] << 8) | (blk_1[count - 46] << 0))) * 0.001;
                double cumulative_mdi_neg_t2 = ((UInt32)((blk_1[count - 44] << 24) | (blk_1[count - 43] << 16) | (blk_1[count - 42] << 8) | (blk_1[count - 41] << 0))) * 0.001;
                double cumulative_mdi_neg_t3 = ((UInt32)((blk_1[count - 39] << 24) | (blk_1[count - 38] << 16) | (blk_1[count - 37] << 8) | (blk_1[count - 36] << 0))) * 0.001;
                double cumulative_mdi_neg_t4 = ((UInt32)((blk_1[count - 34] << 24) | (blk_1[count - 33] << 16) | (blk_1[count - 32] << 8) | (blk_1[count - 31] << 0))) * 0.001;
                double cumulative_mdi_neg_tl = ((UInt32)((blk_1[count - 29] << 24) | (blk_1[count - 28] << 16) | (blk_1[count - 27] << 8) | (blk_1[count - 26] << 0))) * 0.001;

                double cumulative_mdi_abs_t1 = ((UInt32)((blk_1[count - 24] << 24) | (blk_1[count - 23] << 16) | (blk_1[count - 22] << 8) | (blk_1[count - 21] << 0))) * 0.001;
                double cumulative_mdi_abs_t2 = ((UInt32)((blk_1[count - 19] << 24) | (blk_1[count - 18] << 16) | (blk_1[count - 17] << 8) | (blk_1[count - 16] << 0))) * 0.001;
                double cumulative_mdi_abs_t3 = ((UInt32)((blk_1[count - 14] << 24) | (blk_1[count - 13] << 16) | (blk_1[count - 12] << 8) | (blk_1[count - 11] << 0))) * 0.001;
                double cumulative_mdi_abs_t4 = ((UInt32)((blk_1[count - 9] << 24) | (blk_1[count - 8] << 16) | (blk_1[count - 7] << 8) | (blk_1[count - 6] << 0))) * 0.001;
                double cumulative_mdi_abs_tl = ((UInt32)((blk_1[count - 4] << 24) | (blk_1[count - 3] << 16) | (blk_1[count - 2] << 8) | (blk_1[count - 1] << 0))) * 0.001;

                last_active_energy_pos_tl = active_energy_pos_tl;
                last_active_energy_pos_tl_datetime = date_time_b1;
                last_active_energy_neg_tl = active_energy_neg_tl;
                last_active_energy_neg_tl_datetime = date_time_b1;
                last_reactive_energy_pos_tl = reactive_energy_pos_tl;
                last_reactive_energy_pos_tl_datetime = date_time_b1;
                last_reactive_energy_neg_tl = reactive_energy_neg_tl;
                last_reactive_energy_neg_tl_datetime = date_time_b1;

                //MessageBox.Show("INSERT IGNORE INTO meter.billing_data_udil5 VALUES('" + MSN + "', '" + global_device_id + "', '" + date_time_b1 + "', '" + active_energy_pos_t1 + "', '" + active_energy_pos_t2 + "', '" + active_energy_pos_t3 + "', '" + active_energy_pos_t4 + "', '" + active_energy_pos_tl + "', '" + active_energy_neg_t1 + "', '" + active_energy_neg_t2 + "', '" + active_energy_neg_t3 + "', '" + active_energy_neg_t4 + "', '" + active_energy_neg_tl + "', '" + reactive_energy_pos_t1 + "', '" + reactive_energy_pos_t2 + "', '" + reactive_energy_pos_t3 + "', '" + reactive_energy_pos_t4 + "', '" + reactive_energy_pos_tl + "', '" + reactive_energy_neg_t1 + "', '" + reactive_energy_neg_t2 + "', '" + reactive_energy_neg_t3 + "', '" + reactive_energy_neg_t4 + "', '" + reactive_energy_neg_tl + "', '" + active_mdi_pos_t1 + "', '" + active_mdi_pos_t2 + "', '" + active_mdi_pos_t3 + "', '" + active_mdi_pos_t4 + "', '" + active_mdi_pos_tl + "', '" + active_mdi_neg_t1 + "', '" + active_mdi_neg_t2 + "', '" + active_mdi_neg_t3 + "', '" + active_mdi_neg_t4 + "', '" + active_mdi_neg_tl + "', '" + active_mdi_abs_t1 + "', '" + active_mdi_abs_t2 + "', '" + active_mdi_abs_t3 + "', '" + active_mdi_abs_t4 + "', '" + active_mdi_abs_tl + "', '" + cumulative_mdi_pos_t1 + "', '" + cumulative_mdi_pos_t2 + "', '" + cumulative_mdi_pos_t3 + "', '" + cumulative_mdi_pos_t4 + "', '" + cumulative_mdi_pos_tl + "', '" + cumulative_mdi_neg_t1 + "', '" + cumulative_mdi_neg_t2 + "', '" + cumulative_mdi_neg_t3 + "', '" + cumulative_mdi_neg_t4 + "', '" + cumulative_mdi_neg_tl + "', '" + cumulative_mdi_abs_t1 + "', '" + cumulative_mdi_abs_t2 + "', '" + cumulative_mdi_abs_t3 + "', '" + cumulative_mdi_abs_t4 + "', '" + cumulative_mdi_abs_tl + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");
                ExecuteNonQurey("INSERT INTO meter.billing_data_udil5 VALUES('" + MSN + "', '" + global_device_id + "', '" + date_time_b1 + "', '" + active_energy_pos_t1 + "', '" + active_energy_pos_t2 + "', '" + active_energy_pos_t3 + "', '" + active_energy_pos_t4 + "', '" + active_energy_pos_tl + "', '" + active_energy_neg_t1 + "', '" + active_energy_neg_t2 + "', '" + active_energy_neg_t3 + "', '" + active_energy_neg_t4 + "', '" + active_energy_neg_tl + "', '" + reactive_energy_pos_t1 + "', '" + reactive_energy_pos_t2 + "', '" + reactive_energy_pos_t3 + "', '" + reactive_energy_pos_t4 + "', '" + reactive_energy_pos_tl + "', '" + reactive_energy_neg_t1 + "', '" + reactive_energy_neg_t2 + "', '" + reactive_energy_neg_t3 + "', '" + reactive_energy_neg_t4 + "', '" + reactive_energy_neg_tl + "', '" + active_mdi_pos_t1 + "', '" + active_mdi_pos_t2 + "', '" + active_mdi_pos_t3 + "', '" + active_mdi_pos_t4 + "', '" + active_mdi_pos_tl + "', '" + active_mdi_neg_t1 + "', '" + active_mdi_neg_t2 + "', '" + active_mdi_neg_t3 + "', '" + active_mdi_neg_t4 + "', '" + active_mdi_neg_tl + "', '" + active_mdi_abs_t1 + "', '" + active_mdi_abs_t2 + "', '" + active_mdi_abs_t3 + "', '" + active_mdi_abs_t4 + "', '" + active_mdi_abs_tl + "', '" + cumulative_mdi_pos_t1 + "', '" + cumulative_mdi_pos_t2 + "', '" + cumulative_mdi_pos_t3 + "', '" + cumulative_mdi_pos_t4 + "', '" + cumulative_mdi_pos_tl + "', '" + cumulative_mdi_neg_t1 + "', '" + cumulative_mdi_neg_t2 + "', '" + cumulative_mdi_neg_t3 + "', '" + cumulative_mdi_neg_t4 + "', '" + cumulative_mdi_neg_tl + "', '" + cumulative_mdi_abs_t1 + "', '" + cumulative_mdi_abs_t2 + "', '" + cumulative_mdi_abs_t3 + "', '" + cumulative_mdi_abs_t4 + "', '" + cumulative_mdi_abs_tl + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");
            }
            return 0;
        }
        //Checked and updated
        private dynamic update_rssi_value(uint MSN, NetworkStream stream, byte[] data)
        {
            string rssiCommand = "00 01 00 30 00 01 00 0D C0 01 81 00 01 00 00 60 0C 05 FF 02 00";
            byte[] rssi = rssiCommand.Split().Select(s => Convert.ToByte(s, 16)).ToArray();


            int count = 0;
            stream.Write(rssi, 0, rssi.Length);
            count = stream.Read(data, 0, data.Length);
            int c = -1 * (256 - int.Parse((data[count - 1].ToString("X2")), System.Globalization.NumberStyles.HexNumber));
            decimal SignalStrength = c;

            if (data[count - 4] == 9)
            {
                string tech = "GPRS";
                if (data[count - 2] == 2) { tech = "GPRS/EDGE"; }
                if (data[count - 2] == 3) { tech = "WCDMA/HSDPA"; }
                ExecuteNonQurey("UPDATE meter SET rssi ='" + c + "', tech ='" + tech + "' WHERE `serial` ='" + MSN + "';");
            }
            return SignalStrength;
        }
        private void Log_Comm_Sequence(uint MSN, string global_device_id, float longitude, float latitude, string message, DateTime meter_datetime, int comm_status)
        {
            ExecuteNonQurey("INSERT INTO meter.device_communication_history VALUES ('" + MSN + "','" + global_device_id + "','" + meter_datetime.ToString("yyyy/M/d HH:mm:ss") + "', '" + comm_status + "', '" + message + "', '" + longitude + "','" + latitude + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "', '" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "');");
        }
        //CHECKED
       
        private string event_end_code_map(string start_event_code)
        {
            switch (start_event_code)
            {
                case "111":
                    return "112";

                case "113":
                    return "143";

                case "114":
                    return "144";

                case "115":
                    return "145";

                case "116":
                    return "146";

                case "117":
                    return "147";

                case "118":
                    return "148";

                case "203":
                    return "202";

                case "119":
                    return "149";

                case "210":
                    return "211";

                case "217":
                    return "247";

                case "121":
                    return "151";

                default:
                    return "000";
            }
        }
        
        protected string eventCodeTranslation(string code)
        {
            switch (code)
            {
                case "101":
                    return "MDI Reset";

                case "131":
                    return "MDI Reset End";

                case "102":
                    return "Parameterization";

                case "111":
                    return "Power fail start";

                case "112":
                    return "Power fail end";

                case "113":
                    return "Phase failure";

                case "143":
                    return "Phase failure End";

                case "114":
                    return "Over Volt";

                case "144":
                    return "Over Volt End";

                case "115":
                    return "Under Volt";

                case "145":
                    return "Under Volt End";

                case "116":
                    return "Demand Over Load";

                case "146":
                    return "Demand Over Load End";

                case "117":
                    return "Reverse Energy";

                case "147":
                    return "Reverse Energy End";

                case "118":
                    return "Reverse Polarity";

                case "148":
                    return "Reverse Polarity End";

                case "121":
                    return "CT Bypass";

                case "151":
                    return "CT Bypass End";

                case "44":
                    return "Cover Opened";

                case "45":
                    return "Cover Opened End";

                case "54":
                    return "Relay Disconnected";

                case "55":
                    return "Relay Reconnected";

                case "119":
                    return "Reactive Negative Energy";

                case "149":
                    return "Reactive Negative Energy Recovery";

                case "201":
                    return "Time Synchronization";

                case "202":
                    return "Contactor On";

                case "203":
                    return "Contactor Off";

                case "206":
                    return "Door Open";

                case "207":
                    return "Battery Low";

                case "208":
                    return "Memory Failure";

                case "209":
                    return "Meter Tamper";

                case "301":
                    return "Optical Port Login";

                case "302":
                    return "Login with Management Role";

                case "303":
                    return "Sanction Load Control Programmed";

                case "304":
                    return "Load Shedding Schedule Programmed";

                case "305":
                    return "IP Port Programmed";

                case "217":
                    return "Current Unbalance";

                case "218":
                    return "Voltage Unbalance";

                case "247":
                    return "Current Unbalance End";

                case "210":
                    return "Over Current Start";

                case "211":
                    return "Over Current End";

                default:
                    return code;
            }
        }
        private void start_Click(object sender, EventArgs e)
        {
            try
            {
                //IPAddress poi = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Last(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                if (IPBox.Text.Contains('.') && serverPort.Text.Length > 0)
                {
                    server = new TcpListener(IPAddress.Parse(IPBox.Text), int.Parse(serverPort.Text));
                    server.Start();
                    labelServer.Show();
                    labelServer.Text = "Server Started : " + server.Server.LocalEndPoint.ToString() + " @ " + DateTime.Now.ToString();
                    IPBox.Enabled = false;
                    serverPort.Enabled = false;
                    //test();
                }
                else
                {
                    MessageBox.Show("Please select a Valid IP and Port", "T-RECS Suit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                MessageBox.Show("Line #: " + LineNumber + " - " + "Line #: " + LineNumber + " - " + ex.Message);
            }
        }
        private bool timerange(DateTime d1, DateTime d2)
        {
            DateTime Now = DateTime.Parse("4/7/2016 " + DateTime.Now.ToLongTimeString());
            if (Now > d1 && Now < d2)
            {
                return true;
            }
            return false;
        }
        private void DebugData(byte[] data, int size)
        {
            string recorder = DateTime.Now.ToString() + ": ";
            for (int i = 0; i < size; i++)
            {
                recorder += data[i].ToString("X2") + " ";
            }

            Console.WriteLine("\n" + recorder + "\n");
            // listBox_ConnectedClients.Items.Add((object) "\n" + recorder + "\n");
        }
        private void Stop_Click(object sender, EventArgs e)
        {
            try
            {
                server.Stop();
                labelServer.Hide();
            }
            catch (Exception ex)
            {
                var LineNumber = new StackTrace(ex, true).GetFrame(0).GetFileLineNumber();
                MessageBox.Show("Line #: " + LineNumber + " - " + ex.Message);
            }
        }
        private void btnClientService_Click(object sender, EventArgs e)
        {
            StartAccept();
            label1.Text = "Listining for Clients...";
            label1.Show();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() => {
                labelServer.Hide();

            }));
            this.BeginInvoke(new Action(() =>
            {

                labelClient.Hide();
            }));
            this.BeginInvoke(new Action(() =>
            {

                label1.Hide();
            }));
            this.BeginInvoke(new Action(() =>
            {

                labelContime.Hide();
            }));
        }
        private void labelClient_MouseHover(object sender, EventArgs e)
        {
            labelClient.Hide();
        }
        private void tbActivePower_MouseHover(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                tbActivePower.Text = "";
            }));
            this.BeginInvoke(new Action(() =>
            {

                tbReactivePower.Text = "";
            }));
        }
        private void buttonClear_Click(object sender, EventArgs e)
        {
            Console.Clear();
            listBox_ConnectedClients.Items.Clear();
        }
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void reload_Click(object sender, EventArgs e)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (!IPBox.Items.Contains(ip.ToString()))
                    {
                        IPBox.Items.Add(ip.ToString());
                    }
                }
            }
        }
    }
}
