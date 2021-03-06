﻿/*! atomic v1.0.0 | (c) 2015 @toddmotto | https://github.com/toddmotto/atomic */
!function (a, b) { "function" == typeof define && define.amd ? define(b) : "object" == typeof exports ? module.exports = b : a.atomic = b(a) }(this, function (a) { "use strict"; var b = {}, c = { contentType: "application/x-www-form-urlencoded" }, d = function (a) { var b; try { b = JSON.parse(a.responseText) } catch (c) { b = a.responseText } return [b, a] }, e = function (b, e, f) { var g = { success: function () { }, error: function () { }, always: function () { } }, h = a.XMLHttpRequest || ActiveXObject, i = new h("MSXML2.XMLHTTP.3.0"); i.open(b, e, !0), i.setRequestHeader("Content-type", c.contentType), i.onreadystatechange = function () { var a; 4 === i.readyState && (a = d(i), i.status >= 200 && i.status < 300 ? g.success.apply(g, a) : g.error.apply(g, a), g.always.apply(g, a)) }, i.send(f); var j = { success: function (a) { return g.success = a, j }, error: function (a) { return g.error = a, j }, always: function (a) { return g.always = a, j } }; return j }; return b.get = function (a) { return e("GET", a) }, b.put = function (a, b) { return e("PUT", a, b) }, b.post = function (a, b) { return e("POST", a, b) }, b["delete"] = function (a) { return e("DELETE", a) }, b.setContentType = function (a) { c.contentType = a }, b });

(function () {

    var o = "...";
    var displayEl = document.getElementById("runtime_id");
    if (!displayEl) return;

    document.getElementById("restart").onclick = function (e) {
        e.preventDefault();
        atomic.get('/restart.json').success(function() { displayEl.innerText = 'offline ' + o; });
    };

    (function poll(i) {
        setTimeout(function () {
            atomic.get('/ping.json')
                .success(function (data) { o = '...'; displayEl.innerText = data.RuntimeId; })
                .error(function () { o += '...'; displayEl.innerText = 'offline ' + o; })
                .always(function() { poll(250); });
        }, i);
    })(0);

})();
