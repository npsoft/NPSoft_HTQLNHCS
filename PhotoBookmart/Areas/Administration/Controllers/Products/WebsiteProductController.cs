﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.IO;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Newtonsoft.Json;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Controllers;
using PhotoBookmart.DataLayer;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Sites;
using PhotoBookmart.DataLayer.Models.ExtraShipping;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Reports;
using PhotoBookmart.Helper;
using PhotoBookmart.Support;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Admin, RoleEnum.Province, RoleEnum.District, RoleEnum.Village)]
    public class WebsiteProductController : WebAdminController
    {
        [HttpGet]
        public ActionResult Index(int? page)
        {
            DoiTuongSearchModel model = new DoiTuongSearchModel()
            {
                MaHC_Province = CurrentUser.MaHC.GetCodeProvince(),
                MaHC_District = CurrentUser.MaHC.GetCodeDistrict(),
                MaHC_Village = CurrentUser.MaHC.GetCodeVillage(),
                OrderDesc = true,
                Page = page.HasValue ? page.Value : 1
            };
            return View(model);
        }
        
        public ActionResult List(DoiTuongSearchModel model)
        {
            #region Initialize
            JoinSqlBuilder<DoiTuong, DoiTuong> jn = new JoinSqlBuilder<DoiTuong, DoiTuong>();
            SqlExpressionVisitor<DoiTuong> sql_exp = Db.CreateExpression<DoiTuong>();
            var p = PredicateBuilder.True<DoiTuong>();
            #endregion

            #region Where clause
            string ma_hc = string.IsNullOrEmpty(model.MaHC_Village) ? (string.IsNullOrEmpty(model.MaHC_District) ? (string.IsNullOrEmpty(model.MaHC_Province) ? "" : model.MaHC_Province) : model.MaHC_District) : model.MaHC_Village;
            p = p.And(x => x.MaHC.StartsWith(ma_hc));
            if (model.IDDiaChi.HasValue)
            {
                p = p.And(x => x.IDDiaChi == model.IDDiaChi.Value);
            }
            if (!string.IsNullOrEmpty(model.MaLDT))
            {
                p = p.And(x => x.MaLDT == model.MaLDT);
            }
            if (!string.IsNullOrEmpty(model.TinhTrang))
            {
                p = p.And(x => x.TinhTrang == model.TinhTrang);
            }
            if (model.IsDuyet.HasValue)
            {
                p = p.And(x => x.IsDuyet == model.IsDuyet.Value);
            }
            if (!string.IsNullOrEmpty(model.Keywords))
            {
                p = p.And(x => x.HoTen.Contains(model.Keywords));
            }
            #endregion

            #region Order By clause
            jn = jn.Where(p);
            string st = jn.ToSql();
            int idx = st.IndexOf("WHERE");
            sql_exp.SelectExpression = st.Substring(0, idx);
            sql_exp.WhereExpression = string.Format("{0}", st.Substring(idx));
            if (model.OrderDesc)
            {
                switch (model.OrderBy)
                {
                    case "HoTen":
                        sql_exp = sql_exp.OrderByDescending(x => x.HoTen);
                        break;
                    case "NgaySinh":
                        sql_exp = sql_exp.OrderByDescending(x => new { x.NamSinh }).ThenByDescending(x => new { x.ThangSinh }).ThenByDescending(x => new { x.NgaySinh });
                        break;
                    case "GioiTinh":
                        sql_exp = sql_exp.OrderByDescending(x => x.GioiTinh);
                        break;
                    case "MaLDT":
                        sql_exp = sql_exp.OrderByDescending(x => x.MaLDT);
                        break;
                    case "TinhTrang":
                        sql_exp = sql_exp.OrderByDescending(x => x.TinhTrang);
                        break;
                    case "IsDuyet":
                        sql_exp = sql_exp.OrderByDescending(x => x.IsDuyet);
                        break;
                    default:
                        sql_exp = sql_exp.OrderByDescending(x => x.Id);
                        break;
                }
            }
            else
            {
                switch (model.OrderBy)
                {
                    case "HoTen":
                        sql_exp = sql_exp.OrderBy(x => x.HoTen);
                        break;
                    case "NgaySinh":
                        sql_exp = sql_exp.OrderBy(x => new { x.NamSinh }).ThenBy(x => new { x.ThangSinh }).ThenBy(x => new { x.NgaySinh });
                        break;
                    case "GioiTinh":
                        sql_exp = sql_exp.OrderBy(x => x.GioiTinh);
                        break;
                    case "MaLDT":
                        sql_exp = sql_exp.OrderBy(x => x.MaLDT);
                        break;
                    case "TinhTrang":
                        sql_exp = sql_exp.OrderBy(x => x.TinhTrang);
                        break;
                    case "IsDuyet":
                        sql_exp = sql_exp.OrderBy(x => x.IsDuyet);
                        break;
                    default:
                        sql_exp = sql_exp.OrderBy(x => x.Id);
                        break;
                }
            }
            #endregion

            #region Paging (Top) clause
            int pageSize = ITEMS_PER_PAGE;
            int totalItem = (int)Db.Count<DoiTuong>(p);
            int totalPage = (int)Math.Ceiling((double)totalItem / pageSize);
            int currPage = (model.Page > 0 && model.Page < totalPage + 1) ? model.Page : 1;
            sql_exp = sql_exp.Limit((currPage - 1) * pageSize, pageSize);
            #endregion

            #region Retrieve data
            st = sql_exp.ToSelectStatement();
            idx = st.IndexOf("FROM");
            st = currPage > 1 ? string.Format("SELECT * {0}", st.Substring(idx)) : st;
            List<DoiTuong> c = Db.Select<DoiTuong>(st);
            #endregion

            #region Prepare data
            PermissionChecker permission = new PermissionChecker(this);
            List<DanhMuc_LoaiDT> Lst_DanhMuc_LoaiDT = new List<DanhMuc_LoaiDT>();
            List<DanhMuc_TinhTrangDT> Lst_DanhMuc_TinhTrangDT = new List<DanhMuc_TinhTrangDT>();
            List<string> lst_maldt = c.Where(x => !string.IsNullOrEmpty(x.MaLDT)).Select(x => x.MaLDT).Distinct().ToList();
            List<string> lst_tinhtrangdt = c.Where(x => !string.IsNullOrEmpty(x.TinhTrang)).Select(x => x.TinhTrang).Distinct().ToList();
            if (lst_maldt.Count > 0) { Lst_DanhMuc_LoaiDT = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => Sql.In(y.MaLDT, lst_maldt)).Limit(0, lst_maldt.Count)); }
            if (lst_tinhtrangdt.Count > 0) { Lst_DanhMuc_TinhTrangDT = Db.Select<DanhMuc_TinhTrangDT>(x => x.Where(y => Sql.In(y.MaTT, lst_tinhtrangdt)).Limit(0, lst_tinhtrangdt.Count)); }
            c.ForEach(x => {
                x.CanView = permission.CanGet(x);
                x.CanEdit = permission.CanUpdate(x);
                x.CanDelete = permission.CanDelete(x);
                x.MaLDT_Name = string.IsNullOrEmpty(x.MaLDT) ? "" : Lst_DanhMuc_LoaiDT.Single(y => y.MaLDT == x.MaLDT).TenLDT;
                x.TinhTrang_Name = string.IsNullOrEmpty(x.TinhTrang) ? "" : Lst_DanhMuc_TinhTrangDT.Single(y => y.MaTT == x.TinhTrang).TenTT;
            });
            #endregion

            #region Model data
            ViewData["CurrPage"] = currPage;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalItem"] = totalItem;
            ViewData["TotalPage"] = totalPage;
            return PartialView("_List", c);
            #endregion
        }

        [HttpGet]
        public ActionResult Add()
        {
            DoiTuong model = new DoiTuong();
            return View(model);
        }

        [HttpGet]
        public ActionResult Edit(long id)
        {  
            var model = Db.Select<DoiTuong>(x => x.Where(y => y.Id == id).Limit(0, 1)).FirstOrDefault();

            PermissionChecker permission = new PermissionChecker(this);
            if (!permission.CanUpdate(model)) { return RedirectToAction("Index", "WebsiteProduct", new { }); }
            
            model.MaLDT_Details = Db.Where<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == model.IDDT);
            return View("Add", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Update(DoiTuong model, IEnumerable<HttpPostedFileBase> FilesUp)
        {
            #region Refill for object
            model.Id = model.Id > 0 ? model.Id : 0;
            DoiTuong old_model = null;
            if (model.Id > 0)
            {
                old_model = Db.Select<DoiTuong>(x => x.Where(y => y.Id == model.Id).Limit(0, 1)).FirstOrDefault();
                if (old_model != null)
                {
                    model.IDDT = old_model.IDDT;
                    model.CreatedOn = old_model.CreatedOn;
                    model.CreatedBy = old_model.CreatedBy;
                    old_model.MaLDT_Details = Db.Where<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == old_model.IDDT).OrderBy(x => x.Id).ToList();
                }
            }
            else
            {
                model.IDDT = Guid.NewGuid();
                model.CreatedOn = DateTime.Now;
                model.CreatedBy = CurrentUser.Id;
            }
            #endregion

            #region Validate for object
            #region Block #1
            model.HoTen = string.Format("{0}", model.HoTen).Trim();
            model.NamSinh = string.Format("{0}", model.NamSinh).Trim();
            model.ThangSinh = string.Format("{0}", model.ThangSinh).Trim();
            model.NgaySinh = string.Format("{0}", model.NgaySinh).Trim();
            model.TruQuan = string.Format("{0}", model.TruQuan).Trim();
            model.NguyenQuan = string.Format("{0}", model.NguyenQuan).Trim();
            model.MaLDT_Details = model.MaLDT_Details.OrderBy(x => x.Id).ToList();
            if (!model.isKhuyetTat.HasValue || !model.isKhuyetTat.Value)
            {
                model.DangKT = null;
                model.MucDoKT = null;
            }
            #endregion
            #region Block #2
            if (string.IsNullOrEmpty(model.MaHC))
            {
                return JsonError("Vui lòng chọn xã.");
            }
            if (!model.IDDiaChi.HasValue)
            {
                return JsonError("Vui lòng chọn xóm.");
            }
            #endregion
            #region Block #3
            if (string.IsNullOrEmpty(model.HoTen))
            {
                return JsonError("Vui lòng nhập họ & tên.");
            }
            if (string.IsNullOrEmpty(model.NamSinh))
            {
                return JsonError("Vui lòng nhập ngày sinh » Năm.");
            }
            if (!new Regex(@"^([1-9]\d{3})$", RegexOptions.Compiled).IsMatch(model.NamSinh) ||
                int.Parse(model.NamSinh) < DateTime.MinValue.Year ||
                int.Parse(model.NamSinh) > DateTime.MaxValue.Year)
            {
                return JsonError("Ngày sinh » Năm không đúng định dạng.");
            }
            if (!string.IsNullOrEmpty(model.NgaySinh) && string.IsNullOrEmpty(model.ThangSinh))
            {
                return JsonError("Vui lòng nhập ngày sinh » Tháng.");
            }
            if (!string.IsNullOrEmpty(model.ThangSinh) && !new Regex(@"^(0?[1-9]|1[012])$", RegexOptions.Compiled).IsMatch(model.ThangSinh))
            {
                return JsonError("Ngày sinh » Tháng không đúng định dạng.");
            }
            if (!string.IsNullOrEmpty(model.NgaySinh))
            {
                DateTime dt = new DateTime(int.Parse(model.NamSinh), int.Parse(model.ThangSinh), 1).AddMonths(1).Subtract(TimeSpan.FromSeconds(1));
                if (!new Regex(@"^(0?[1-9]|[12][0-9]|3[01])$", RegexOptions.Compiled).IsMatch(model.NgaySinh) || int.Parse(model.NgaySinh) > dt.Day)
                {
                    return JsonError("Ngày sinh » Ngày không đúng định dạng.");
                }
            }
            model.ThangSinh = !string.IsNullOrEmpty(model.ThangSinh) && model.ThangSinh.Length < 2 ? "0" + model.ThangSinh : model.ThangSinh;
            model.NgaySinh = !string.IsNullOrEmpty(model.NgaySinh) && model.NgaySinh.Length < 2 ? "0" + model.NgaySinh : model.NgaySinh;
            if (string.IsNullOrEmpty(model.GioiTinh))
            {
                return JsonError("Vui lòng chọn giới tính.");
            }
            if (string.IsNullOrEmpty(model.TruQuan))
            {
                return JsonError("Vui lòng nhập trú quán.");
            }
            if (string.IsNullOrEmpty(model.NguyenQuan))
            {
                return JsonError("Vui lòng nhập nguyên quán.");
            }
            #endregion
            #region Block #4
            if (string.IsNullOrEmpty(model.MaLDT))
            {
                return JsonError("Vui lòng chọn loại.");
            }
            if (model.MaLDT.StartsWith("01"))
            {
                #region TODO: 01
                if (model.MaLDT_Details.Count != 1)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                detail.Type1_InfoFather = string.Format("{0}", model.MaLDT_Details[0].Type1_InfoFather).Trim();
                detail.Type1_InfoMother = string.Format("{0}", model.MaLDT_Details[0].Type1_InfoMother).Trim();
                if (string.IsNullOrEmpty(detail.Type1_InfoFather))
                {
                    return JsonError("Vui lòng nhập thông tin cha.");
                }
                if (string.IsNullOrEmpty(detail.Type1_InfoMother))
                {
                    return JsonError("Vui lòng nhập thông tin mẹ.");
                }
                model.MaLDT_Details[0] = detail;
                #endregion
            }
            else if (model.MaLDT.StartsWith("03"))
            {
                #region TODO: 03
                if (model.MaLDT_Details.Count == 0)
                {
                    return JsonError("Vui lòng thêm thông tin » con.");
                }
                else if (model.MaLDT.StartsWith("0301") && model.MaLDT_Details.Count != 1)
                {
                    return JsonError("Loại 3.1 giành cho đối tượng nuôi 1 con.");
                }
                else if (model.MaLDT.StartsWith("0302") && model.MaLDT_Details.Count < 2)
                {
                    return JsonError("Loại 3.2 giành cho đối tượng nuôi 2 con trở lên.");
                }
                List<DoiTuong_LoaiDoiTuong_CT> details = new List<DoiTuong_LoaiDoiTuong_CT>();
                foreach (DoiTuong_LoaiDoiTuong_CT item in model.MaLDT_Details)
                {
                    DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                    detail.Id = item.Id;
                    detail.Type3_FullName = string.Format("{0}", item.Type3_FullName).Trim();
                    detail.Type3_DateOfBirth = item.Type3_DateOfBirth;
                    detail.Type3_DateOfBirth_IsMonth = item.Type3_DateOfBirth_IsMonth;
                    detail.Type3_DateOfBirth_IsDate = item.Type3_DateOfBirth_IsDate;
                    detail.Type3_Gender = item.Type3_Gender;
                    detail.Type3_CurrAddr = string.Format("{0}", item.Type3_CurrAddr).Trim();
                    detail.Type3_StatusLearn = string.Format("{0}", item.Type3_StatusLearn).Trim();
                    if (string.IsNullOrEmpty(detail.Type3_FullName))
                    {
                        return JsonError("Vui lòng kiểm tra lại họ & tên cho các con.");
                    }
                    if (detail.Type3_DateOfBirth.Year == DateTime.MinValue.Year)
                    {
                        return JsonError("Vui lòng kiểm tra lại ngày sinh cho các con » Năm.");
                    }
                    if (!detail.Type3_DateOfBirth_IsMonth && detail.Type3_DateOfBirth.Month != 1)
                    {
                        return JsonError("Vui lòng kiểm tra lại ngày sinh cho các con » Tháng.");
                    }
                    if (!detail.Type3_DateOfBirth_IsDate && detail.Type3_DateOfBirth.Day != 1)
                    {
                        return JsonError("Vui lòng kiểm tra lại ngày sinh cho các con » Ngày.");
                    }
                    if (string.IsNullOrEmpty(detail.Type3_Gender) || !new string[] { "Male", "Female" }.Contains(detail.Type3_Gender))
                    {
                        return JsonError("Vui lòng kiểm tra lại giới tính cho các con.");
                    }
                    details.Add(detail);
                }
                model.MaLDT_Details = details;
                if (model.Id == 0 && details.Count(x => x.Id > 0) > 0)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                #endregion
            }
            else if (model.MaLDT.StartsWith("04"))
            {
                #region TODO: 04
                if (model.MaLDT_Details.Count != 1)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                detail.Type4_MaritalStatus = model.MaLDT_Details[0].Type4_MaritalStatus;
                detail.Type4_InfoAdditional = string.Format("{0}", model.MaLDT_Details[0].Type4_InfoAdditional).Trim();
                if (string.IsNullOrEmpty(detail.Type4_MaritalStatus))
                {
                    return JsonError("Vui lòng chọn tình trạng hôn nhân.");
                };
                model.MaLDT_Details[0] = detail;
                #endregion
            }
            else if (model.MaLDT.StartsWith("05"))
            {
                #region TODO: 05
                if (model.MaLDT_Details.Count != 1)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                detail.Type5_SelfServing = model.MaLDT_Details[0].Type5_SelfServing;
                detail.Type5_Carer = string.Format("{0}", model.MaLDT_Details[0].Type5_Carer).Trim();
                if (string.IsNullOrEmpty(detail.Type5_SelfServing))
                {
                    return JsonError("Vui lòng chọn khả năng phục vụ.");
                }
                model.MaLDT_Details[0] = detail;
                #endregion
            }
            else
            {
                #region TODO: Others
                if (model.MaLDT_Details.Count > 0)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                #endregion
            }
            if (model.Id > 0 && model.MaLDT_Details.Count(x => x.Id > 0) > 0 && model.MaLDT_Details.Count(x => x.Id > 0) != Db.Count<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == model.IDDT && Sql.In(x.Id, model.MaLDT_Details.Where(y => y.Id > 0).Select(y => y.Id))))
            {
                return JsonError("Vui lòng không hack ứng dụng.");
            }
            if (model.isKhuyetTat.HasValue && model.isKhuyetTat.Value)
            {
                if (!model.DangKT.HasValue)
                {
                    return JsonError("Vui lòng chọn dạng khuyết tật.");
                }
                if (!model.MucDoKT.HasValue)
                {
                    return JsonError("Vui lòng chọn mức độ khuyết tật.");
                }
            }
            #endregion
            #region Block #5
            if (!model.MucTC.HasValue)
            {
                return JsonError("Vui lòng nhập mức trợ cấp.");
            }
            if (model.MucTC.Value < 0)
            {
                return JsonError("Mức trợ cấp không đúng định dạng.");
            }
            if (!model.NgayHuong.HasValue)
            {
                return JsonError("Vui lòng chọn ngày hưởng.");
            }
            if (string.IsNullOrEmpty(model.TinhTrang))
            {
                return JsonError("Vui lòng chọn tình trạng.");
            }
            #endregion
            #region Block #6
            List<long> MaLDT_Details_Ids = model.MaLDT_Details.Where(x => x.Id > 0).Select(x => x.Id).ToList(); 
            if (Db.Count<DanhMuc_HanhChinh>(x => x.MaHC == model.MaHC) == 0 ||
                Db.Count<DanhMuc_DiaChi>(x => x.MaHC == model.MaHC && x.IDDiaChi == model.IDDiaChi) == 0 ||
                !new string[] { "Male", "Female" }.Contains(model.GioiTinh) ||
                Db.Count<DanhMuc_LoaiDT>(x => x.MaLDT == model.MaLDT) == 0 ||
                Db.Count<DanhMuc_TinhTrangDT>(x => x.MaTT == model.TinhTrang) == 0 ||
                model.DangKT.HasValue && Db.Count<DanhMuc_DangKhuyetTat>(x => x.IDDangTat == model.DangKT.Value) == 0 ||
                model.MucDoKT.HasValue && Db.Count<DanhMuc_MucDoKhuyetTat>(x => x.IDMucDoKT == model.MucDoKT.Value) == 0 ||
                model.MaDanToc.HasValue && Db.Count<DanhMuc_DanToc>(x => x.Id == model.MaDanToc.Value) == 0 ||
                model.Id > 0 && MaLDT_Details_Ids.Count > 0 && MaLDT_Details_Ids.Count != Db.Count<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == model.IDDT && Sql.In(x.Id, MaLDT_Details_Ids)) ||
                (!model.IsDuyet || model.Id == 0) && !GetTinhTrangDTsByParams(false).Select(x => x.MaTT).Contains(model.TinhTrang) ||
                (model.Id > 0 && old_model.IsDuyet && !model.IsDuyet) || (model.IsDuyet && (model.Id == 0 || !old_model.IsDuyet) && (RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) == RoleEnum.Village))
            {
                return JsonError("Vui lòng không hack ứng dụng.");
            }
            #endregion
            #region Block #7
            DanhMuc_LoaiDT loaidt = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => y.MaLDT == model.MaLDT).Limit(0, 1)).FirstOrDefault();
            if (!model.MaLDT.CheckDateOfBirth(model.NamSinh, model.ThangSinh, model.NgaySinh))
            {
                return JsonError("Ngày sinh không phù hợp với loại.");
            }

            if (model.Id == 0 && model.IsDuyet ||
                model.Id > 0 && model.IsDuyet && !old_model.IsDuyet)
            {
                DoiTuong_BienDong bien_dong = new DoiTuong_BienDong();
                bien_dong.MaHC = model.MaHC;
                bien_dong.IDDiaChi = model.IDDiaChi;
                bien_dong.TinhTrang = model.TinhTrang;
                bien_dong.MaLDT = model.MaLDT;
                bien_dong.NgayHuong = model.NgayHuong;
                bien_dong.HeSo = decimal.Parse(string.Format("{0}", loaidt.HeSo));
                bien_dong.MucTC = model.MucTC;
                bien_dong.MucChenh = model.MucTC;
                model.BienDong_Lst_Ins.Add(bien_dong);
            }

            if (model.Id > 0 && old_model.IsDuyet)
            {
                #region Initialize miscellaneous
                int sobd = GetSBD(model.Id);
                bool is_change_details = true;
                if (old_model.MaLDT == model.MaLDT &&
                    old_model.MaLDT_Details.Count == model.MaLDT_Details.Count)
                {
                    if (model.MaLDT.StartsWith("01"))
                    {
                        if (old_model.MaLDT_Details[0].Type1_InfoFather == model.MaLDT_Details[0].Type1_InfoFather &&
                            old_model.MaLDT_Details[0].Type1_InfoMother == model.MaLDT_Details[0].Type1_InfoMother)
                        {
                            is_change_details = false;
                        }
                    }
                    else if (model.MaLDT.StartsWith("03"))
                    {
                        for (int i = 0; i < model.MaLDT_Details.Count; i++)
                        {
                            if (old_model.MaLDT_Details[i].Id == model.MaLDT_Details[i].Id ||
                                old_model.MaLDT_Details[i].Type3_FullName == model.MaLDT_Details[i].Type3_FullName ||
                                old_model.MaLDT_Details[i].Type3_DateOfBirth == model.MaLDT_Details[i].Type3_DateOfBirth ||
                                old_model.MaLDT_Details[i].Type3_DateOfBirth_IsMonth == model.MaLDT_Details[i].Type3_DateOfBirth_IsMonth ||
                                old_model.MaLDT_Details[i].Type3_DateOfBirth_IsDate == model.MaLDT_Details[i].Type3_DateOfBirth_IsDate ||
                                old_model.MaLDT_Details[i].Type3_Gender == model.MaLDT_Details[i].Type3_Gender ||
                                old_model.MaLDT_Details[i].Type3_CurrAddr == model.MaLDT_Details[i].Type3_CurrAddr ||
                                old_model.MaLDT_Details[i].Type3_StatusLearn == model.MaLDT_Details[i].Type3_StatusLearn)
                            {
                                break;
                            }
                        }
                        is_change_details = false;
                    }
                    else if (model.MaLDT.StartsWith("04"))
                    {
                        if (old_model.MaLDT_Details[0].Type4_MaritalStatus == model.MaLDT_Details[0].Type4_MaritalStatus &&
                            old_model.MaLDT_Details[0].Type4_InfoAdditional == model.MaLDT_Details[0].Type4_InfoAdditional)
                        {
                            is_change_details = false;
                        }
                    }
                    else if (model.MaLDT.StartsWith("05"))
                    {
                        if (old_model.MaLDT_Details[0].Type5_SelfServing == model.MaLDT_Details[0].Type5_SelfServing &&
                            old_model.MaLDT_Details[0].Type5_Carer == model.MaLDT_Details[0].Type5_Carer)
                        {
                            is_change_details = false;
                        }
                    }
                    else
                    {
                        is_change_details = false;
                    }
                }
                #endregion

                #region Validate miscellaneous
                if (is_change_details)
                {
                    if (model.IsThayDoiDoChuyenLoaiDoiTuong)
                    {
                        DateTime dt_old = new DateTime(old_model.NgayHuong.Value.Year, old_model.NgayHuong.Value.Month, 1, 0, 0, 0, 0);
                        DateTime dt_new = new DateTime(model.NgayHuong.Value.Year, model.NgayHuong.Value.Month, 1, 0, 0, 0, 0);
                        if (dt_new <= dt_old)
                        {
                            return JsonError("Tháng biến động phải lớn hơn tháng đang hưởng.");
                        }
                        DoiTuong_BienDong bien_dong_them = new DoiTuong_BienDong();
                        DoiTuong_BienDong bien_dong_cat = new DoiTuong_BienDong();
                        bien_dong_them.MaHC = bien_dong_cat.MaHC = model.MaHC;
                        bien_dong_them.IDDiaChi = bien_dong_cat.IDDiaChi = model.IDDiaChi;
                        bien_dong_them.TinhTrang = bien_dong_cat.TinhTrang = model.TinhTrang;
                        bien_dong_them.MaLDT = bien_dong_cat.MaLDT = model.MaLDT;
                        bien_dong_them.NgayHuong = bien_dong_cat.NgayHuong = model.NgayHuong;
                        bien_dong_them.HeSo = bien_dong_cat.HeSo = decimal.Parse(string.Format("{0}", loaidt.HeSo)); 
                        decimal muc_chenh = (decimal)(model.MucTC - old_model.MucTC);
                        bien_dong_them.MucTC = bien_dong_cat.MucTC = model.MucTC;
                        bien_dong_them.MucChenh = bien_dong_cat.MucChenh = muc_chenh > 0 ? muc_chenh : -muc_chenh;
                        if (model.MucTC > old_model.MucTC)
                        {
                            // bien_dong_them.LoaiBD = "HCT";
                            // bien_dong_cat.LoaiBD = "KCT";
                            bien_dong_them.MoTa = "Thêm do chuyển loại trợ cấp tăng";
                            bien_dong_cat.MoTa = "Cắt do chuyển loại trợ cấp tăng";
                        }
                        else if (model.MucTC < old_model.MucTC)
                        {
                            // bien_dong_them.LoaiBD = "HCG";
                            // bien_dong_cat.LoaiBD = "KCG";
                            bien_dong_them.MoTa = "Thêm do chuyển loại trợ cấp giảm";
                            bien_dong_cat.MoTa = "Cắt do chuyển loại trợ cấp giảm";
                        }
                        else
                        {
                            // bien_dong_them.LoaiBD = "HCK";
                            // bien_dong_cat.LoaiBD = "KCK";
                            bien_dong_them.MoTa = "Thêm do chuyển loại trợ cấp";
                            bien_dong_cat.MoTa = "Cắt do chuyển loại trợ cấp";
                        }
                        model.BienDong_Lst_Ins.Add(bien_dong_them);
                        model.BienDong_Lst_Ins.Add(bien_dong_cat);
                    }
                    else
                    {
                        if (sobd != 1)
                        {
                            return JsonError("Số biến động hiện tại khác 1.");
                        }
                        if (old_model.NgayHuong != model.NgayHuong)
                        {
                            return JsonError("Vui lòng xóa hết lịch sử biến động trước khi sửa.");
                        }
                        DoiTuong_BienDong bien_dong = Db.Select<DoiTuong_BienDong>(x => x.Where(y => y.IDDT == model.Id).Limit(0, 1)).FirstOrDefault();
                        bien_dong.MaHC = model.MaHC;
                        bien_dong.IDDiaChi = model.IDDiaChi;
                        bien_dong.TinhTrang = model.TinhTrang;
                        bien_dong.MaLDT = model.MaLDT;
                        bien_dong.NgayHuong = model.NgayHuong;
                        bien_dong.HeSo = decimal.Parse(string.Format("{0}", loaidt.HeSo));
                        bien_dong.MucChenh = model.MucTC - bien_dong.MucTC;
                        bien_dong.MucTC = model.MucTC;
                        bien_dong.MucChenh = bien_dong.MucChenh > 0 ? bien_dong.MucChenh : -bien_dong.MucChenh;
                        model.BienDong_Lst_Upd.Add(bien_dong);
                    }
                }
                else
                {
                    if (sobd > 1 && old_model.NgayHuong != model.NgayHuong)
                    {
                        return JsonError("Ngày hưởng mới và ngày hưởng gốc khác nhau.");
                    }
                    DoiTuong_BienDong bien_dong = Db.Select<DoiTuong_BienDong>(x => x.Where(y => y.IDDT == model.Id).Limit(0, 1)).FirstOrDefault();
                    bien_dong.MaHC = model.MaHC;
                    bien_dong.IDDiaChi = model.IDDiaChi;
                    bien_dong.TinhTrang = model.TinhTrang;
                    bien_dong.MaLDT = model.MaLDT;
                    bien_dong.NgayHuong = model.NgayHuong;
                    bien_dong.HeSo = decimal.Parse(string.Format("{0}", loaidt.HeSo));
                    bien_dong.MucChenh = model.MucTC - bien_dong.MucTC;
                    bien_dong.MucTC = model.MucTC;
                    bien_dong.MucChenh = bien_dong.MucChenh > 0 ? bien_dong.MucChenh : -bien_dong.MucChenh;
                    model.BienDong_Lst_Upd.Add(bien_dong);
                }
                #endregion
            }
            #endregion
            #region Block #8
            PermissionChecker permission = new PermissionChecker(this);
            if (!(model.Id == 0 && permission.CanAdd(model) ||
                  model.Id > 0 && permission.CanUpdate(old_model) && permission.CanUpdate(model)))
            {
                return JsonError("Vui lòng không hack ứng dụng.");
            }
            #endregion
            #endregion
            
            #region Save changes to database
            using (IDbTransaction dbTrans = Db.OpenTransaction())
            {
                Db.Delete<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == model.IDDT && x.CodeType != model.MaLDT);
                if (model.MaLDT.StartsWith("03"))
                {
                    List<long> ids = new List<long>() { 0 };
                    ids.AddRange(model.MaLDT_Details.Where(x => x.Id > 0).Select(x => x.Id));
                    Db.Delete<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == model.IDDT && !Sql.In(x.Id, ids));
                }

                if (!model.MaLDT.StartsWith("03") && model.MaLDT_Details.Count == 1)
                {
                    DoiTuong_LoaiDoiTuong_CT detail = Db.Select<DoiTuong_LoaiDoiTuong_CT>(x => x.Where(y => y.CodeObj == model.IDDT).Limit(0, 1)).FirstOrDefault();
                    if (detail != null) { model.MaLDT_Details[0].Id = detail.Id; }
                }
                model.MaLDT_Details.ForEach(x => {
                    x.CodeObj = model.IDDT;
                    x.CodeType = model.MaLDT;
                });

                Db.Save(model);
                if (model.Id == 0) { model.Id = Db.GetLastInsertId(); }
                model.BienDong_Lst_Ins.ForEach(x => {
                    x.IDDT = model.Id;
                });
                Db.UpdateAll<DoiTuong_BienDong>(model.BienDong_Lst_Upd);
                Db.InsertAll<DoiTuong_BienDong>(model.BienDong_Lst_Ins);
                Db.UpdateAll<DoiTuong_LoaiDoiTuong_CT>(model.MaLDT_Details.Where(x => x.Id > 0));
                Db.InsertAll<DoiTuong_LoaiDoiTuong_CT>(model.MaLDT_Details.Where(x => x.Id == 0));
                dbTrans.Commit();
            }
            return JsonSuccess(Url.Action("Index", "WebsiteProduct", new { }), null);
            #endregion
        }
        
        public ActionResult Delete(int id)
        {
            try
            {
                PermissionChecker permission = new PermissionChecker(this);
                var entity = Db.Select<DoiTuong>(x => x.Where(y => y.Id == id).Limit(0, 1)).FirstOrDefault();
                if (!permission.CanDelete(entity))
                {
                    return JsonError("Vui lòng không hack ứng dụng");
                }
                using (IDbTransaction dbTrans = Db.OpenTransaction())
                {
                    Db.Delete<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == entity.IDDT);
                    Db.DeleteById<DoiTuong>(id);
                    dbTrans.Commit();
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Detail(int id)
        {
            var model = Db.Where<Product>(m => m.Id == id).FirstOrDefault();
            if (model == null)
                return Redirect("/");


            // created by username
            var list_users = Cache_GetAllUsers();

            var zk = list_users.Where(m => m.Id == model.CreatedBy).FirstOrDefault();
            if (zk == null)
            {
                model.CreatedByUsername = "Deleted User";
            }
            else
            {
                if (string.IsNullOrEmpty(zk.FullName))
                    model.CreatedByUsername = zk.UserName;
                else
                    model.CreatedByUsername = zk.FullName;
            }

            return View(model);
        }

        public ActionResult Move(long id, int move)
        {
            try
            {
                var e = Db.SelectParam<Product>(m => (m.Id == id)).FirstOrDefault();

                var a = new List<Product>();

                var t = new Product();

                if (move == 1)
                {
                    a = Db.Where<Product>(m => (m.CatId == e.CatId && m.Order < e.Order)).OrderBy(m => (m.Order)).ToList();

                    if (a.Count != 0) t = a.LastOrDefault();
                }
                else
                {
                    a = Db.Where<Product>(m => (m.CatId == e.CatId && m.Order > e.Order)).OrderBy(m => (m.Order)).ToList();

                    if (a.Count != 0) t = a.FirstOrDefault();
                }

                if (t.Id > 0)
                {
                    int i = t.Order;

                    t.Order = e.Order;

                    e.Order = i;

                    Db.Update<Product>(t);

                    Db.Update<Product>(e);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }
        
        #region Detail Option in Product

        /// <param name="id">Site ID</param>
        /// <returns></returns>
        public ActionResult Detail_Option_List(int id)
        {
            var c = Db.Where<OptionInProduct>(m => m.ProductId == id);

            // created by username
            var list_users = Cache_GetAllUsers();
            var options = Db.Select<Product_Option>();

            foreach (var x in c)
            {
                var z = list_users.Where(m => m.Id == x.CreatedBy);
                if (z.Count() > 0)
                {
                    var k = z.First();
                    if (string.IsNullOrEmpty(k.FullName))
                        x.CreatedByUsername = k.UserName;
                    else
                        x.CreatedByUsername = k.FullName;
                }
                else
                {
                    x.CreatedByUsername = "Deleted user";
                }

                var sk = options.Where(m => m.Id == x.ProductOptionId).FirstOrDefault();
                if (sk != null)
                {
                    x.Option_Name = sk.InternalName;
                }
            }

            return PartialView(c);
        }

        public ActionResult Detail_Option_Add(long product_id)
        {
            // check product exist
            var product = Db.Select<Product>(x => x.Where(m => m.Id == product_id).Limit(1)).FirstOrDefault();
            if (product == null)
            {
                return Redirect("/");
            }

            var model = new OptionInProduct();
            model.ProductId = product.Id;
            model.Product_Name = product.Name;

            return View(model);
        }

        public ActionResult Detail_Option_Edit(long id)
        {
            var model = Db.Select<OptionInProduct>(x => x.Where(m => m.Id == id).Limit(1)).FirstOrDefault();
            if (model == null)
            {
                return Redirect("/");
            }

            // get product name and option name 
            var product = Db.Select<Product>(x => x.Where(m => m.Id == model.ProductId).Limit(1)).FirstOrDefault();
            if (product == null)
            {
                model.Product_Name = "Deleted product";
            }
            else
            {
                model.Product_Name = product.Name;
            }

            var option = Db.Select<Product_Option>(x => x.Where(m => m.Id == model.ProductOptionId).Limit(1)).FirstOrDefault();
            if (option == null)
            {
                model.Option_Name = "Deleted option";
            }
            else
            {
                model.Option_Name = option.Name;
            }

            return View("Detail_Option_Add", model);
        }


        public ActionResult Detail_Option_Update(OptionInProduct model)
        {
            var curent_item = new OptionInProduct();
            if (model.Id > 0)
            {
                curent_item = Db.Where<OptionInProduct>(m => m.Id == model.Id).FirstOrDefault();
                if (curent_item == null)
                {
                    return Redirect("/");
                }
            }
            else
            {
                // if we add new, make sure no dupplication
                var x = Db.Where<OptionInProduct>(m => m.ProductOptionId == model.ProductOptionId && m.ProductId == model.ProductId);
                if (x.Count > 0)
                {
                    return JsonError("Duplicated option.");
                }
                curent_item.CreatedBy = AuthenticatedUserID;
                curent_item.CreatedOn = DateTime.Now;
            }

            curent_item.ProductId = model.ProductId;
            curent_item.ProductOptionId = model.ProductOptionId;

            curent_item.isRequire = model.isRequire;
            curent_item.DefaultQuantity = model.DefaultQuantity;
            curent_item.MaxQuantity = model.MaxQuantity;
            curent_item.MinQuantity = model.MinQuantity;
            curent_item.CanApplyCoupon = model.CanApplyCoupon;

            if (model.Id > 0)
            {
                Db.Update<OptionInProduct>(curent_item);
            }
            else
            {
                Db.Insert<OptionInProduct>(curent_item);
            }
            return JsonSuccess(Url.Action("Detail", new { id = curent_item.ProductId }));
        }

        public ActionResult Detail_Option_Delete(int id)
        {
            try
            {
                Db.DeleteById<OptionInProduct>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Detail Image

        /// <param name="id">Site ID</param>
        /// <returns></returns>
        public ActionResult Detail_Image_List(int id)
        {
            var c = Db.Where<Product_Images>(m => m.ProductId == id);

            // created by username
            var list_users = Cache_GetAllUsers();
            var list_cat = Cache_GetProductCategory();

            foreach (var x in c)
            {
                var z = list_users.Where(m => m.Id == x.CreatedBy);
                if (z.Count() > 0)
                {
                    var k = z.First();
                    if (string.IsNullOrEmpty(k.FullName))
                        x.CreatedByUsername = k.UserName;
                    else
                        x.CreatedByUsername = k.FullName;
                }
                else
                {
                    x.CreatedByUsername = "Deleted user";
                }
            }

            return PartialView(c);
        }

        public ActionResult Detail_Image_Add(Product_Images model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            var curent_item = new Product_Images();
            if (model.Id > 0)
            {
                curent_item = Db.Where<Product_Images>(m => m.Id == model.Id).FirstOrDefault();
                if (curent_item == null)
                {
                    return JsonError("Please dont try to hack us");
                }
            }
            else
            {
                curent_item.CreatedBy = AuthenticatedUserID;
                curent_item.CreatedOn = DateTime.Now;
            }

            curent_item.ProductId = model.ProductId;
            curent_item.Name = model.Name;
            curent_item.Status = model.Status;

            if (FileUp != null && FileUp.FirstOrDefault() != null)
            {
                curent_item.Filename = UploadFile(AuthenticatedUserID, model.ProductId.ToString(), "ProductImage", FileUp);
            }

            if (model.Id > 0)
            {
                Db.Update<Product_Images>(curent_item);
            }
            else
            {
                Db.Insert<Product_Images>(curent_item);
            }
            return RedirectToAction("Detail", new { id = model.ProductId });
        }

        public ActionResult Detail_Image_Delete(int id)
        {
            try
            {
                var x = Db.Where<Product_Images>(m => m.Id == id).FirstOrDefault();
                if (x != null)
                {
                    var path = Server.MapPath("~/" + x.Filename);
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }
                Db.DeleteById<Product_Images>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Support
        
        [HttpPost]
        public ActionResult GetEthnicsForFilter()
        {
            return Json(Cache_GetAllEthnics());
        }
        
        [HttpPost]
        public ActionResult GetLevelsDisabilityForFilter()
        {
            return Json(Cache_GetAllLevelsDisability());
        }
        
        [HttpPost]
        public ActionResult GetMaritalStatusesForFilter()
        {
            return Json(Cache_GetAllMaritalStatuses());
        }

        [HttpPost]
        public ActionResult GetSelfServingsForFilter()
        {
            return Json(Cache_GetAllSelfServings());
        }

        private ActionResult ExportListProduct()
        {
            var package = new ExcelPackage();

            package.Workbook.Worksheets.Add("Products");
            ExcelWorksheet ws = package.Workbook.Worksheets[1];
            ws.Name = "Products"; //Setting Sheet's name
            ws.Cells.Style.Font.Size = 12; //Default font size for whole sheet
            ws.Cells.Style.Font.Name = "Calibri"; //Default Font name for whole sheet

            //Merging cells and create a center heading for out table
            ws.Cells[1, 1].Value = "List of Photobookmart Products "; // Heading Name
            ws.Cells[1, 1].Style.Font.Size = 22;
            ws.Cells[1, 1, 1, 10].Merge = true; //Merge columns start and end range
            ws.Cells[1, 1, 1, 10].Style.Font.Bold = true; //Font should be bold
            ws.Cells[1, 1, 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Aligmnet is center


            var lstProductCat = Db.Select<Product_Category>(x => x.Where(y => (y.Status)));

            var lstProductOption = Db.Select<Product_Option>(x => x.Where(y => (y.Status)));


            List<string> headers = new List<string>() { "", "Name", "Size", "Pages", "Price", "Shipping", "PhotoCreation_Id" };

            headers.AddRange(lstProductOption.Select(x => (x.InternalName)).ToArray<string>());

            int row = 3;

            for (int i = 0; i < headers.Count; i++)
            {
                ws.Cells[row, i + 1].Value = headers[i];

                ws.Cells[row, i + 1].Style.Font.Bold = true;

                ws.Cells[row, i + 1].Style.Font.Size = 13;
            }
            //

            FillAllExcel(ref ws, lstProductCat, lstProductOption, headers.Count);


            for (int i = 0; i < headers.Count; i++)
            {
                if (i < headers.Count - lstProductOption.Count)
                {
                    ws.Column(i + 1).AutoFit();
                }
                else
                {
                    ws.Column(i + 1).Width = 25;
                }
            }

            // footer

            ws.View.FreezePanes(3, 7);

            var memoryStream = package.GetAsByteArray();
            package.Dispose();
            var fileName = string.Format("List product {0:yyyy-MM-dd-HH-mm-ss}.xlsx", DateTime.Now);
            package.Dispose();
            return base.File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

        }

        private void FillAllExcel(ref ExcelWorksheet ws, List<Product_Category> lstProductCat, List<Product_Option> lstProductOption, int header_count)
        {
            var row = 4;

            foreach (var pc in lstProductCat ?? Enumerable.Empty<Product_Category>())
            {
                InitRowCategory(ref ws, pc, row++, header_count);

                var lstProduct = Db.Select<Product>(x => x.Where(y => (y.Status && y.CatId == pc.Id)).OrderBy(z => (z.Order)));
                int _p_index = 1;
                foreach (var p in lstProduct ?? Enumerable.Empty<Product>())
                {
                    InitRowProduct(ref ws, p, row++, lstProductOption, _p_index);
                    _p_index++;
                }

                // footer
                //row++;
                ws.Cells[row, 2].Value = "Total";
                ws.Cells[row, 2].Style.Font.Bold = true;
                ws.Cells[row, 2].Style.Font.Italic = true;
                ws.Cells[row, 2].Style.Font.Size = 11;
                ws.Cells[row, 2, row, 3].Merge = true; //Merge columns start and end range

                ws.Cells[row, 4].Value = lstProduct == null ? 0 : lstProduct.Count;
                row++; row++;
            }


        }

        private void InitRowCategory(ref ExcelWorksheet ws, Product_Category cat, int row, int numOfCols)
        {
            ws.Cells[row, 1].Value = cat.Name;

            ws.Cells[row, 1].Style.Font.Bold = true;

            ws.Cells[row, 1].Style.Font.Size = 11;

            ws.Cells[row, 1, row, numOfCols].Merge = true;
            ws.Cells[row, 1, row, numOfCols].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, numOfCols].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(230, 232, 235));
        }

        private void InitRowProduct(ref ExcelWorksheet ws, Product product, int row, List<Product_Option> lstProductOption, int product_index)
        {
            var rate = Setting_GetExchangeRate();
            ws.Cells[row, 1].Value = product_index;
            ws.Cells[row, 2].Value = product.Name;

            ws.Cells[row, 3].Value = product.Size;

            ws.Cells[row, 4].Value = string.Format("{0} Pages", product.Pages);

            ws.Cells[row, 5].Value = product.getPrice(Enum_Price_MasterType.Product, rate.Code).Value.ToMoneyFormated(rate.CurrencyCode);

            ws.Cells[row, 6].Value = product.isFreeShip ? "Free" : product.getPrice(Enum_Price_MasterType.ProductShippingPrice,rate.Code).Value.ToMoneyFormated(rate.CurrencyCode);

            ws.Cells[row, 7].Value = product.MyPhotoCreationId;

            var lstProductInOption = Db.Select<OptionInProduct>(x => x.Where(y => (y.ProductId == product.Id)).OrderBy(z => (z.Id)));

            for (int i = 0; i < lstProductOption.Count; i++)
            {
                var productInOption = lstProductInOption.Where(x => (x.ProductOptionId == lstProductOption[i].Id)).FirstOrDefault();

                ws.Cells[row, 7 + i + 1].Value = productInOption != null ? lstProductOption[i].getPrice(Enum_Price_MasterType.ProductOption,rate.Code).Value.ToMoneyFormated(rate.CurrencyCode) : "";
            }
        }

        #endregion
    }
}
