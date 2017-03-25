﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpiralEdge.Helper;

namespace SpiralEdge.Model
{
    public class ConfigModel
    {
        #region For: Configuration for connection
        public string CONFIG_CONN_STRING { get; set; }
        public int CONFIG_CONN_TIMEOUT { get; set; }
        public int CONFIG_CONN_TIMEOUT_TIMES { get; set; }
        public int CONFIG_CONN_TIMEOUT_INCREMENT { get; set; }
        public int CONFIG_CONN_PAGE_SIZE { get; set; }
        #endregion
        #region For: Configuration for path, email
        public string CONFIG_EMAIL_USER { get; set; }
        public string CONFIG_EMAIL_PASS { get; set; }
        public string[] CONFIG_EMAIL_RECEIVED { get; set; }
        public string[] CONFIG_EMAIL_RECEIVED_EX { get; set; }
        public string CONFIG_PATH_LOG { get; set; }
        public string CONFIG_PATH_CONFIG { get; set; }
        public DateTime CONFIG_REPORT_TIME { get; set; }
        #endregion
        #region For: Configuration for local, server
        public string CONFIG_DAFANBA_USER { get; set; }
        public string CONFIG_DAFANBA_PASS { get; set; }
        public string CONFIG_DAFANBA_URL_DEFAULT { get; set; }
        public string CONFIG_DAFANBA_URL_LIVEDEALER { get; set; }
        public string CONFIG_DAFANBA_DIR_PRINT { get; set; }
        public int CONFIG_DAFANBA_INTERVAL_CAPTURE_AG { get; set; }
        public int CONFIG_DAFANBA_ALERT_LENGTH_AG { get; set; }
        #endregion
        #region For: Configuration for schedule, settings
        #endregion
        #region For: Configuration for report, other settings
        public bool Test { get; set; }
        public RBLog Log { get; set; }
        public SQLiteHelper ConnHelper { get; set; }
        public List<DB_AGIN_Baccarat> LatestAGINs { get; set; }
        #endregion
        
        private void Init()
        {
            string s; DateTime dt;
            #region For: Initialize for connection
            CONFIG_CONN_STRING = ConfigurationManager.AppSettings["Conn_String"];
            CONFIG_CONN_TIMEOUT = int.Parse(ConfigurationManager.AppSettings["Conn_Timeout"]);
            CONFIG_CONN_TIMEOUT_TIMES = 3;
            CONFIG_CONN_TIMEOUT_INCREMENT = 5 * 60;
            CONFIG_CONN_PAGE_SIZE = int.Parse(ConfigurationManager.AppSettings["Conn_Page_Size"]);
            #endregion
            #region For: Initialize for path, email
            CONFIG_EMAIL_USER = ConfigurationManager.AppSettings["Email_User"];
            CONFIG_EMAIL_PASS = ConfigurationManager.AppSettings["Email_Pass"];
            CONFIG_EMAIL_RECEIVED = ConfigurationManager.AppSettings["Email_Recieved"].Split(new string[1] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            CONFIG_EMAIL_RECEIVED_EX = ConfigurationManager.AppSettings["Email_Recieved_Ex"].Split(new string[1] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            CONFIG_PATH_LOG = ConfigurationManager.AppSettings["Path_Log"];
            CONFIG_PATH_CONFIG = ConfigurationManager.AppSettings["Path_Config"];
            CONFIG_REPORT_TIME = DateTime.Now;
            s = ConfigurationManager.AppSettings["Report_Time"];
            if (!string.IsNullOrEmpty(s))
            {
                DateTime.TryParseExact(s, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt);
                CONFIG_REPORT_TIME = dt;
            }
            #endregion
            #region For: Initialize for local, server
            CONFIG_DAFANBA_USER = ConfigurationManager.AppSettings["Dafanba_User"];
            CONFIG_DAFANBA_PASS = ConfigurationManager.AppSettings["Dafanba_Pass"];
            CONFIG_DAFANBA_URL_DEFAULT = ConfigurationManager.AppSettings["Dafanba_Url_Default"];
            CONFIG_DAFANBA_URL_LIVEDEALER = ConfigurationManager.AppSettings["Dafanba_Url_LiveDealer"];
            CONFIG_DAFANBA_DIR_PRINT = ConfigurationManager.AppSettings["Dafanba_Dir_Print"];
            CONFIG_DAFANBA_INTERVAL_CAPTURE_AG = int.Parse(ConfigurationManager.AppSettings["Dafanba_Interval_Capture_AG"]) * 1000;
            CONFIG_DAFANBA_ALERT_LENGTH_AG = int.Parse(ConfigurationManager.AppSettings["Dafanba_Alert_Length_AG"]);
            #endregion
            #region For: Initialize for schedule, settings
            #endregion
            #region For: Initialize for report, other settings
            Test = "1" == ConfigurationManager.AppSettings["Test"];
            Log = new RBLog(CONFIG_PATH_LOG);
            ConnHelper = new SQLiteHelper(CONFIG_CONN_STRING);
            InitAGINs();
            Log.Log(string.Format("Information\t:: ------------------ APPLICATION STARTING -----------------------"));
            #endregion
        }

        private void InitAGINs()
        {
            LatestAGINs = new List<DB_AGIN_Baccarat>();
            SQLiteCommand cmd = ConnHelper.ConnDb.CreateCommand();
            try
            {
                #region SQLiteCommand: Initialize
                #region cmd.CommandText = string.Format(@"")
                cmd.CommandText = string.Format(@"
SELECT A.*
FROM AGIN A
WHERE Id IN (SELECT MAX(Id) FROM AGIN GROUP BY CoordinateX, CoordinateY)");
                #endregion
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = CONFIG_CONN_TIMEOUT;
                #endregion
                #region SQLiteCommand: Parameters
                #endregion
                #region SQLiteCommand: Connection
                DataSet ds = ConnHelper.ExecCmd(cmd);
                #endregion
                #region For: Retrieve
                DataTable dt = ds.Tables[0];
                List<string> cols = new List<string>();
                foreach (DataColumn dc in dt.Columns)
                {
                    cols.Add(dc.ColumnName);
                }
                foreach (DataRow dr in dt.Rows)
                {
                    LatestAGINs.Add(DB_AGIN_Baccarat.ExtractDB(dr, cols));
                }
                #endregion
                #region For: Clean
                dt.Clear();
                ds.Clear();
                dt.Dispose();
                ds.Dispose();
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("{0}{1}", ex.Message, ex.StackTrace), ex);
            }
            finally
            {
                cmd.Dispose();
            }
        }
        
        public ConfigModel() { }

        public ConfigModel(bool isInit = true)
        {
            if (isInit) { Init(); }
        }

        public void HdlAGIN(string name)
        {
            string file_path = Path.Combine(CONFIG_DAFANBA_DIR_PRINT, name);
            if (File.Exists(file_path))
            {
                AGIN_3840x2160_Baccarat agin_3840x2160_baccarat = null;
                ImageHelper.AnalysisImg_AGIN_3840x2160(file_path, out agin_3840x2160_baccarat);

                List<DB_AGIN_Baccarat> agins_img = DB_AGIN_Baccarat.ExtractImg(agin_3840x2160_baccarat, name, DateTime.Now, 0, DateTime.Now, 0);
                foreach (DB_AGIN_Baccarat agin_img in agins_img)
                {
                    DB_AGIN_Baccarat agin_latest = LatestAGINs.Where(x => x.CoordinateX == agin_img.CoordinateX && x.CoordinateY == agin_img.CoordinateY).FirstOrDefault();
                    #region For: Save/Clean baccarat
                    agin_img.SaveDbTrack(ConnHelper);
                    if (0 != agin_img.DataAnalysis.TotalInvalid) { continue; }
                    agin_img.Id = 0;
                    agin_img.DataAnalysis.DelEmpty();
                    #endregion
                    #region For: Merge baccarat
                    bool merged = false;
                    if (null != agin_latest)
                    {
                        int dist = DB_AGIN_Baccarat_Tbl.DistMerge(
                            agin_latest.DataAnalysis, agin_img.DataAnalysis,
                            DB_AGIN_Baccarat_Tbl.DistMax(agin_latest.DataAnalysis, agin_img.DataAnalysis));
                        if (-1 != dist)
                        {
                            DB_AGIN_Baccarat_Tbl.ExecMerge(agin_latest.DataAnalysis, agin_img.DataAnalysis, dist);
                            agin_latest.FileNames = Regex.Replace(agin_latest.FileNames + agin_img.FileNames, @"(;;)", ";");
                            agin_latest.LastModifiedOn = agin_img.LastModifiedOn;
                            agin_latest.LastModifiedBy = agin_img.LastModifiedBy;
                            merged = true;
                        }
                    }
                    #endregion
                    #region For: Add baccarat
                    if (!merged)
                    {
                        if (null != agin_latest)
                        {
                            LatestAGINs.Remove(agin_latest);
                        }
                        LatestAGINs.Add(agin_img);
                        agin_latest = LatestAGINs.Single(x => x.CoordinateX == agin_img.CoordinateX && x.CoordinateY == agin_img.CoordinateY);
                    }
                    #endregion
                    #region For: Order baccarat
                    agin_latest.DataAnalysis.UpdOrder(
                        agin_latest.DataAnalysis.LatestOrder, agin_latest.DataAnalysis.LatestOrderCircle,
                        agin_latest.DataAnalysis.LatestOrderX, agin_latest.DataAnalysis.LatestOrderY,
                        agin_latest.DataAnalysis.LatestOrderXR, agin_latest.DataAnalysis.LatestOrderYR);
                    #endregion
                    #region For: Alert via pattern(s)
                    int pattern01 = agin_latest.ChkPattern01();
                    if (CONFIG_DAFANBA_ALERT_LENGTH_AG <= pattern01 && !agin_latest.AlertPattern01)
                    {
                        AlertAGIN(agin_latest.CoordinateX, agin_latest.CoordinateY, agin_latest.DataAnalysis.LatestOrderCircle, pattern01, file_path);
                    }
                    agin_latest.AlertPattern01 = CONFIG_DAFANBA_ALERT_LENGTH_AG <= pattern01;
                    #endregion
                    #region For: Save data to database
                    agin_latest.SaveDb(ConnHelper);
                    #endregion
                }
            }
        }

        public void AlertAGIN(int x, int y, string type, int length, string attachment)
        {
            Log.Log(string.Format("Information\t:: Alert processing has been started."));
            #region For: Write log
            Log.Log(string.Format("Information\t:: [ (x,y) = ({0},{1}), type = {2}, length = {3}, attachment = {4} ]", x, y, type, length, attachment));
            #endregion
            #region For: Send email(s)
            string subject = string.Format("[BET - DAFANBA - AGIN] ({0},{1}) {2} = {3} | {4:yyyy-MM-dd HH:mm:ss}", x, y, type, length, DateTime.Now);
            string content = string.Format(@"
            Hi you,<br/><br/>
            Please review alert for AGIN | {4:yyyy-MM-dd HH:mm:ss}:<br/>
            <ul style='padding:0'>
                <li>[ (x,y) = ({0},{1}), type = {2}, length: {3} ]</li>
            </ul><br/>
            Best regards!", x, y, type, length, DateTime.Now);
            string[] attachments = new string[1] { attachment };
            string display_name = "BET TOOL";
            MailHelper mail_helper = new MailHelper(CONFIG_EMAIL_USER, CONFIG_EMAIL_PASS);
            string send_email = mail_helper.SendEmail(CONFIG_EMAIL_RECEIVED, subject, content, attachments, display_name);
            Log.Log(string.Format("Information\t:: [ send email(s) = {0} ]", send_email));
            #endregion
            Log.Log(string.Format("Information\t:: Alert processing has been completed."));
        }

        public void SendEmail(string mainContent = null)
        {
            Log.Log(string.Format("Information\t:: Send email(s) processing has been started."));
            string subject = string.Format("[BET - TOOL] REPORT | {0:yyyy-MM-dd HH:mm:ss}", CONFIG_REPORT_TIME);
            string content = string.Format(@"
            Hi you,<br/><br/>
            Please review report for {0:yyyy-MM-dd HH:mm:ss}:<br/><ul style='padding:0;'>", CONFIG_REPORT_TIME);
            content += string.Format("{0}", mainContent);
            content += string.Format("</ul><br/>Best regards!");
            string[] attachments = null;
            string display_name = "BET TOOL";
            MailHelper mail_helper = new MailHelper(CONFIG_EMAIL_USER, CONFIG_EMAIL_PASS);
            string send_email = mail_helper.SendEmail(CONFIG_EMAIL_RECEIVED, subject, content, attachments, display_name);
            Log.Log(string.Format("Information\t:: Send email(s) processing has been completed."));
        }
        
        public void Ex170323_HdlAGIN()
        {
            List<DB_AGIN_Baccarat> agins = new List<DB_AGIN_Baccarat>();
            SQLiteCommand cmd = ConnHelper.ConnDb.CreateCommand();
            try
            {
                #region SQLiteCommand: Initialize
                #region cmd.CommandText = string.Format(@"")
                cmd.CommandText = string.Format(@"
SELECT T.*
FROM AGIN T
WHERE FileName IN ('agin-170318-215633-059.png', 'agin-170318-215702-621.png', 'agin-170318-215719-358.png', 'agin-170318-215735-731.png', 'agin-170318-215754-723.png', 'agin-170318-215820-010.png', 'agin-170318-215836-532.png', 'agin-170318-215853-198.png', 'agin-170318-215922-232.png', 'agin-170318-215939-653.png', 'agin-170318-215956-323.png', 'agin-170318-220019-930.png', 'agin-170318-220038-924.png', 'agin-170318-220055-380.png', 'agin-170318-220112-110.png', 'agin-170318-220141-792.png', 'agin-170318-220158-709.png', 'agin-170318-220215-153.png', 'agin-170318-220237-210.png', 'agin-170318-220259-457.png')
  AND CoordinateX = 1 AND CoordinateY = 1
ORDER BY Id ASC");
                #endregion
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = CONFIG_CONN_TIMEOUT;
                #endregion
                #region SQLiteCommand: Parameters
                // -: cmd.Parameters.Add(new SQLiteParameter() { Value = "agin-170318-213613-937.png" });
                #endregion
                #region SQLiteCommand: Connection
                DataSet ds = ConnHelper.ExecCmd(cmd);
                #endregion
                #region For: Retrieve
                DataTable dt = ds.Tables[0];
                List<string> cols = new List<string>();
                foreach (DataColumn dc in dt.Columns)
                {
                    cols.Add(dc.ColumnName);
                }
                foreach (DataRow dr in dt.Rows)
                {
                    agins.Add(DB_AGIN_Baccarat.Ex170323_ExtractDB(dr, cols));
                    agins.Last().Id = 0;
                    agins.Last().DataAnalysis.DelEmpty();
                    // agins.Last().DataAnalysis.UpdOrder(agins.Last().DataAnalysis.LatestOrder, agins.Last().DataAnalysis.LatestOrderCircle, agins.Last().DataAnalysis.LatestOrderX, agins.Last().DataAnalysis.LatestOrderY, agins.Last().DataAnalysis.LatestOrderXR, agins.Last().DataAnalysis.LatestOrderYR);
                }
                #endregion
                #region For: Clean
                dt.Clear();
                ds.Clear();
                dt.Dispose();
                ds.Dispose();
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("{0}{1}", ex.Message, ex.StackTrace), ex);
            }
            finally
            {
                cmd.Dispose();
            }
            foreach (DB_AGIN_Baccarat agin in agins)
            {
                agin.SaveDbTrack(ConnHelper);
            }
            var agin_org = agins[0];
            agin_org.DataAnalysis.UpdOrder(agin_org.DataAnalysis.LatestOrder, agin_org.DataAnalysis.LatestOrderCircle, agin_org.DataAnalysis.LatestOrderX, agin_org.DataAnalysis.LatestOrderY, agin_org.DataAnalysis.LatestOrderXR, agin_org.DataAnalysis.LatestOrderYR);
            agin_org.Id = 0;
            agin_org.SaveDbTrack(ConnHelper);
            /* -: for (int i = 1; i < agins.Count; i++)
            {*/
                var agin_new = agins[agins.Count - 1];
                int dist_max = DB_AGIN_Baccarat_Tbl.DistMax(agin_org.DataAnalysis, agin_new.DataAnalysis);
                int dist = DB_AGIN_Baccarat_Tbl.DistMerge(agin_org.DataAnalysis, agin_new.DataAnalysis, dist_max);
                Log.Log(string.Format("Information\t:: [ i: {0}, dist-max: {1}, dist: {2} ]", agins.Count/*i + 1*/, dist_max, dist));
                if (-1 != dist)
                {
                    DB_AGIN_Baccarat_Tbl.ExecMerge(agin_org.DataAnalysis, agin_new.DataAnalysis, dist);
                    agin_org.FileNames = Regex.Replace(agin_org.FileNames + agin_new.FileNames, @"(;;)", ";");
                    agin_org.LastModifiedOn = agin_new.LastModifiedOn;
                    agin_org.LastModifiedBy = agin_new.LastModifiedBy;
                    agin_org.DataAnalysis.UpdOrder(agin_org.DataAnalysis.LatestOrder, agin_org.DataAnalysis.LatestOrderCircle, agin_org.DataAnalysis.LatestOrderX, agin_org.DataAnalysis.LatestOrderY, agin_org.DataAnalysis.LatestOrderXR, agin_org.DataAnalysis.LatestOrderYR);
                    agin_org.Id = 0;
                    agin_org.SaveDbTrack(ConnHelper);
                }
            /* -: }*/
        }

        public static void SendEmailEx(ConfigModel config, Exception ex)
        {
            try
            {
                string user = null != config ? config.CONFIG_EMAIL_USER : "{$USER}";
                string pass = null != config ? config.CONFIG_EMAIL_PASS : "{$PASS}";
                string[] emails = config != null ? config.CONFIG_EMAIL_RECEIVED_EX : new string[1] { "npe.etc@gmail.com" };
                string subject = string.Format("[BET - TOOL] EXCEPTION | {0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
                string content = string.Format(@"
                Hi you,<br/><br/>
                Important exception occurred, here is content:<br/><br/>
                Message: {0}<br/><br/>
                StackTrace: {1}<br/><br/>
                Please review,", ex.Message, ex.StackTrace);
                string[] attachments = null;
                string display_name = "BET TOOL";
                MailHelper mail_helper = new MailHelper(user, pass);
                string send_email = mail_helper.SendEmail(emails, subject, content, attachments, display_name);
            }
            catch { }
        }
    }
}
