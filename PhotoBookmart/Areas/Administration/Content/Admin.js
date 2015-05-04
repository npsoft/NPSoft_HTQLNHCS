﻿/// Administration Panel Javascript
// Travel To Go project
// Copyright: Trung Dang (trungdt@absoft.vn)

var mainList_Website = [];

jQuery(document).ready(function ($) {

    $("input[type='text'].date-of-birth-year").spinner({
        min: 1000,
        max: 9999,
        step: 1,
        numberFormat: "n"
    });

    $("input[type='text'].date-of-birth-month").spinner({
        min: 1,
        max: 12,
        step: 1,
        numberFormat: "n"
    });

    $("input[type='text'].date-of-birth-date").spinner({
        min: 1,
        max: 31,
        step: 1,
        numberFormat: "n"
    });

    $("input[type='text'].currency").spinner({
        min: 0,
        step: 100000.00,
        numberFormat: "n"
    });

    $("form").on("reset", function (e) {
        var $frm = $(this);
        setTimeout(function (e) {
            $frm.find("select.mws-select2[data-init]").each(function (index, element) {
                $element = $(element);
                $element.select2().select2("val", $element.attr("data-init"));
            });
        }, 100);
    });
});

jQuery(document).ajaxStart(function () {
    show_loading();
});

jQuery(document).ajaxStop(function () {
    hide_loading();
});

/// This function will reload the Main List Website on the Sidebar
function mainList_ReloadWebsite(dis_id, callback) {
    // reload 
    var html = "";
    html += "<optgroup label='Active'>";
    for (var i = 0; i < mainList_Website.length; i++) {
        var item = mainList_Website[i];
        if (item.DisId != dis_id || item.Status != 1) {
            continue;
        }
        html += "<option value=" + item.Id + ">" + item.Name + "</option>";
    }
    html += "</optgroup>";

    html += "<optgroup label='Non-active'>";
    for (var i = 0; i < mainList_Website.length; i++) {
        var item = mainList_Website[i];
        if (item.DisId != dis_id || item.Status == 1) {
            continue;
        }
        html += "<option value=" + item.Id + ">" + item.Name + "</option>";
    }
    html += "</optgroup>";

    jQuery("#MainList_Website").select2("destroy").html(html).select2();

    mainList_WebsiteChange();

    if (callback != null) {
        callback();
    }
}

function site_reload(wait) {
    if (wait == null) {
        window.location.href = window.location.href;
    }
    else {
        setTimeout(function () {
            window.location.href = window.location.href;
        }, wait);
    }
}

// Parse working time from int to hh:mm
function WorkingTimeParse(t) {
    var h = Math.floor(t / 60);
    var m = t % 60;
    var pmam = "AM";
    if (h > 11) {
        h = h - 12;
        pmam = "PM";
    }

    if (h < 10) {
        h = "0" + h;
    }
    if (m < 10) {
        m = "0" + m;
    }

    return ret = h + ":" + m + " " + pmam;
}

function getUrlParameter(_param, _url) {
    var sPageURL = window.location.search.substring(1);
    if (typeof (_url) !== "undefined") {
        var index = _url.indexOf("?");
        sPageURL = index != -1 ? _url.substring(index + 1) : "";
    }
    var sURLVariables = sPageURL.split("&");
    for (var i = 0; i < sURLVariables.length; i++) {
        var sParameterName = sURLVariables[i].split("=");
        if (sParameterName[0] == _param) {
            return sParameterName[1];
        }
    }
};

/* Sta: Functions support for form */
$.fn.serializeObject = function()
{
    var o = {};
    var a = this.serializeArray();
    $.each(a, function() {
        if (o[this.name] !== undefined) {
            if (!o[this.name].push) {
                o[this.name] = [o[this.name]];
            }
            o[this.name].push(this.value || '');
        } else {
            o[this.name] = this.value || '';
        }
    });
    return o;
};
/* End: Functions support for form */

/* Sta: Functions support for string */
if (!String.prototype.format) {
    String.prototype.format = function () {
        var args = arguments;
        return this.replace(/{(\d+)}/g, function (match, number) {
            return typeof args[number] != 'undefined'
              ? args[number]
              : match
            ;
        });
    };
}

function NewGuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
};

function IsNullOrEmpty(_data) {
    return _data == undefined || _data == null || _data == "";
};
/* End: Functions support for string */

/* Sta: Functions support for time */
function ParseTime(_data) {
    if (typeof _data !== "undefined") {
        // Template: Date object
        if (typeof _data == "object") {
            return _data;
        }
        // Template: Number object
        if (typeof _data == "number") {
            return new Date(_data);
        }
        // Template: String object {
        if (typeof _data == "string") {
            // Template: "\/Date(INTEGER)\/"
            if (/^\/Date\(\d+\)\/$/g.test(_data)) {
                var str = _data.replace(/\/Date\((-?\d+)\)\//, '$1');
                return new Date(parseInt(str));
            }
            // Template: "\/Date(INTEGER[+|-]INTEGER)\/"
            if (/^\/Date\(\d+[\+\-]\d+\)\/$/g.test(_data)) {
                var isPlus = true;
                var idx = _data.indexOf("+");
                if (/^\/Date\(\d+\-\d+\)\/$/g.test(_data)) {
                    isPlus = false;
                    idx = _data.indexOf("-");
                }
                var offset = parseInt(_data.substr(idx + 1, 2)) * 60 + parseInt(_data.substr(idx + 3, 2));
                var time = parseInt(_data.replace(/[\+\-]\d{4}/, "").replace(/\/Date\((-?\d+)\)\//, '$1'));
                return new Date(new Date(time).getTime() + ((new Date()).getTimezoneOffset() - (isPlus ? -offset : offset)) * 60 * 1000);
            }
            // Template: "dd/MM/yyyy"
            if (/^(0?[1-9]|[12][0-9]|3[01])[\/](0?[1-9]|1[012])[\/]([1-9]\d{3})$/.test(_data)) {
                var arr = _data.split("/");
                var y = parseInt(arr[2]);
                var m = parseInt(arr[1]);
                var d = parseInt(arr[0]);
                var dt = new Date(y, m, 1, 0, 0, -1);
                return d > dt.getDate() ? null : new Date(y, m - 1, d, 0, 0, 0, 0);
            }
        }
    }
    return null;
};

function ConvertTime(_data) {
    if (!(typeof _data.tzOffsetOrg !== "undefined")) {
        _data["tzOffsetOrg"] = 0;
    }
    if (!(typeof _data.tzOffsetNew !== "undefined")) {
        _data["tzOffsetNew"] = 0 - (new Date()).getTimezoneOffset();
    }
    _data.dtTime = ParseTime(_data.dtTime);

    return new Date(_data.dtTime.getTime() + (_data.tzOffsetNew - _data.tzOffsetOrg) * 60 * 1000);
};

function TimeForReq(_data) {
    if (typeof _data.dt !== "object" || _data.dt == null) {
        return null;
    }
    if (typeof _data.tz !== "number") {
        var d = _data.dt.getDate();
        var m = _data.dt.getMonth() + 1;
        var y = _data.dt.getFullYear();
        return (m > 9 ? m.toString() : "0" + m.toString()) + "/" + (d > 9 ? d.toString() : "0" + d.toString()) + "/" + y;
    }
    var offset = -(new Date().getTimezoneOffset() + _data.tz);
    return new Date(_data.dt.getTime() + offset * 60 * 1000);
};

function CheckDateOfBirth(_data) {
    var $txt_year = _data.$txt_year;
    var $txt_month = _data.$txt_month;
    var $txt_date = _data.$txt_date;

    var year = $.trim($txt_year.val());
    var month = $.trim($txt_month.val());
    var date = $.trim($txt_date.val());

    if (IsNullOrEmpty(year)) {
        notify_error("Lỗi", "Vui lòng nhập ngày sinh » Năm.");
        $txt_year.focus();
        return false;
    } else {
        // TODO: Need check range of year in MS SQL Server
        var min_year = 1000, max_year = 9999; // Are you sure?
        if (!/^([1-9]\d{3})$/.test(year) || parseInt(year) < min_year || parseInt(year) > max_year) {
            notify_error("Lỗi", "Ngày sinh » Năm không đúng định dạng.");
            $txt_year.focus();
            return false;
        }
    }

    if (!IsNullOrEmpty(date) && IsNullOrEmpty(month)) {
        notify_error("Lỗi", "Vui lòng nhập ngày sinh » Tháng.")
        $txt_month.focus();
        return false;
    }
    if (!IsNullOrEmpty(month) && !/^(0?[1-9]|1[012])$/.test(month)) {
        notify_error("Lỗi", "Ngày sinh » Tháng không đúng định dạng.")
        $txt_month.focus();
        return false;
    }

    if (!IsNullOrEmpty(date)) {
        var dt = new Date(parseInt(year), parseInt(month), 1, 0, 0, -1);
        if (!/^(0?[1-9]|[12][0-9]|3[01])$/.test(date) || parseInt(date) > dt.getDate()) {
            notify_error("Lỗi", "Ngày sinh » Ngày không đúng định dạng.")
            $txt_date.focus();
            return false;
        }
    }

    month = !IsNullOrEmpty(month) && month.length < 2 ? "0" + month : month;
    date = !IsNullOrEmpty(date) && date.length < 2 ? "0" + date : date
    $txt_year.val(year);
    $txt_month.val(month);
    $txt_date.val(date);
    return true;
};
/* End: Functions support for time */
