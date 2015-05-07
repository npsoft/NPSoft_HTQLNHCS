﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Net;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.DataLayer.Models.System
{
    public enum Enum_Settings_DataType
    {
        Raw,
        Int,
        Double,
        Percent,
        String,
        DateTime
    }

    public enum Enum_Settings_Scope
    {
        [Display(Name = "Cả nước")]
        Country,
        [Display(Name = "Cấp tỉnh")]
        Province,
        [Display(Name = "Cấp huyện")]
        District
    }

    /// <summary>
    /// Define keys to be use to store the settings
    /// </summary>
    public enum Enum_Settings_Key
    {
        NONE,
        
        [Display(Name = "Người ký các quyết định")]
        TL01,
        [Display(Name = "Mức trợ cấp cơ bản")]
        TL02,
        [Display(Name = "Mức đóng BHYT")]
        TL03,
        [Display(Name = "Lương cơ bản")]
        TL04,
        [Display(Name = "Ngày áp dụng mức trợ cấp")]
        TL05,
        [Display(Name = "Chức danh người ký")]
        TL06,
        [Display(Name = "Mức trợ cấp mai táng phí")]
        TL07,
        [Display(Name = "Hiển thị ngày/tháng sinh trong DS chi trả")]
        TL10,
        [Display(Name = "Mức trợ cấp cho người nghèo")]
        TL11,
        
        #region SMS
        SMS_SERVICE_ENABLE,
        SMS_SERVICE_URL,
        SMS_SERVICE_USERNAME,
        SMS_SERVICE_PASSWORD,
        #endregion

        #region Site Settings
        WEBSITE_CURRENCY,
        WEBSITE_CURRENCY_FORMAT,
        WEBSITE_ADDITIONAL_PAGE_NAME,
        WEBSITE_GST_ENABLE,
        #endregion

        #region File Settings
        /// <summary>
        /// Location for customer to upload files in case corrupt
        /// </summary>
        WEBSITE_CUSTOMER_UPLOAD_PATH_DEFAULT,
        /// <summary>
        /// Place to keep all orders folder which already PAID
        /// </summary>
        WEBSITE_UPLOAD_PATH_DEFAULT,
        /// <summary>
        /// Location to keep all orders folder which is not yet PAY
        /// </summary>
        WEBSITE_ORDERS_FOLDER_NOTYETPAID_PATH,

        /// <summary>
        /// Path to folder which the Digilabs will look into for the Input DGL for decrypt
        /// </summary>
        WEBSITE_DGL_AUTODECRYPT_INPUT,

        /// <summary>
        /// Path to folder which the digilabs will put the output of the decryption
        /// </summary>
        WEBSITE_DGL_AUTODECRYPT_OUTPUT,

        #endregion

        #region Task Settings

        TASK_AUTO_CANCEL_ORDER_IF_MORETHAN_MINUTE

        #endregion
    }

    [Alias("Settings")]
    [Schema("System")]
    public partial class Settings
    {
        [PrimaryKey]
        [AutoIncrement]
        [IgnoreWhenGenerateList]
        public int Id { get; set; }
        [Default(typeof(string), "")]
        public string MaHC { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Desc { get; set; }
        [Ignore]
        public string Code_Province { get; set; }
        [Ignore]
        public string Code_District { get; set; }
        [Ignore]
        public string Name_Province { get; set; }
        [Ignore]
        public string Name_District { get; set; }
        [Ignore]
        public bool CanEdit { get; set; }
        [Ignore]
        public bool CanDelete { get; set; }
        /// <summary>
        /// Get a setting by name
        /// </summary>
        /// <returns>Cast the return to your specific datatype</returns>
        public static object Get(string key, object default_value = null, Enum_Settings_DataType data_type = Enum_Settings_DataType.Raw)
        {
            var db = ModelBase.ServiceAppHost.TryResolve<IDbConnection>();
            if (db.State != ConnectionState.Open)
            {
                db = ModelBase.ServiceAppHost.TryResolve<IDbConnectionFactory>().Open();
            }

            string value = "";

            try
            {
                var k = db.Select<Settings>(x => x.Where(m => m.Key == key).Limit(1)).FirstOrDefault();
                if (k == null)
                {
                    value = default_value.ToString();
                }
                else
                {
                    value = k.Value;
                }

                // // we dispose the connection to save resource
                db.Close();

                switch (data_type)
                {
                    case Enum_Settings_DataType.Int:
                        return int.Parse(value.ToString());

                    case Enum_Settings_DataType.Double:
                        return double.Parse(value.ToString());

                    case Enum_Settings_DataType.Percent:
                        value = value.Substring(0, value.Length - 1);
                        return double.Parse(value);

                    case Enum_Settings_DataType.String:
                        return value.ToString();

                    case Enum_Settings_DataType.DateTime:
                        return DateTime.Parse(value.ToString());

                    case Enum_Settings_DataType.Raw:
                        return k;
                    default:
                        return value;
                }

            }
            catch (Exception ex)
            {
                return default_value;
            }
        }

        public static object Get(Enum_Settings_Key key, object default_value = null, Enum_Settings_DataType data_type = Enum_Settings_DataType.Raw)
        {
            return Get(key.ToString(), default_value, data_type);
        }

        /// <summary>
        /// Set data into the db
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="data_type"></param>
        public static void Set(Enum_Settings_Key key, object value, Enum_Settings_DataType data_type = Enum_Settings_DataType.Raw)
        {
            var Db = ModelBase.ServiceAppHost.TryResolve<IDbConnection>();

            if (Db.State != ConnectionState.Open)
            {
                Db = ModelBase.ServiceAppHost.TryResolve<IDbConnectionFactory>().Open();
            }


            // try to get it first
            var key_name = key.ToString();
            var item = Db.Select<Settings>(x => x.Where(m => m.Key == key_name).Limit(1)).FirstOrDefault();
            if (item == null)
            {
                // insert new
                item = new Settings() { Key = key_name, Value = value.ToString() };
                Db.Insert<Settings>(item);
            }
            else
            {
                // update
                item.Value = value.ToString();
                Db.Update<Settings>(item);
            }
            // we dispose the connection to save resource
            Db.Close();
        }
    }
}