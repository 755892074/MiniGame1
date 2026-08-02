/* === DOUYIN LOG RELAY SHIM (auto-injected, do not edit by hand) === */
(function () {
  // 兼容抖音小游戏全局对象（GameGlobal）与普通浏览器（globalThis/window）
  var G = (typeof GameGlobal !== 'undefined') ? GameGlobal
        : (typeof globalThis !== 'undefined') ? globalThis
        : this;
  var RELAY_URL = "http://127.0.0.1:18765/log";
  var _buf = [];
  var _timer = null;
  var _closed = false;
  var _dead = false;            // 连续多次失败后才彻底停手，避免单次瞬断误判
  var _failCount = 0;
  var MAX_FAIL = 6;

  function _markFail() {
    _failCount++;
    if (_failCount >= MAX_FAIL) {
      _dead = true;
      try { console.warn('[LogRelay] 本地日志上报持续失败（疑似域名白名单拦截），已停止重试。'
        + '解决：开发者工具勾选「不校验合法域名」，或在 project.config.json 加 "urlCheck":false'); } catch (e) {}
    }
  }
  function _markOk() { _failCount = 0; }

  // 发送单条：优先 GET(query，抖音小游戏最友好)，超长或失败再 POST(body)
  function _sendOne(level, msg) {
    if (_dead) return;
    level = level || "log";
    msg = msg || "";
    try {
      if (msg.length <= 1500) {
        var q = "?level=" + encodeURIComponent(level) + "&msg=" + encodeURIComponent(msg);
        if (typeof tt !== 'undefined' && tt.request) {
          tt.request({
            url: RELAY_URL + q, method: 'GET',
            success: function () { _markOk(); },
            fail: function () { _markFail(); }
          });
          return;
        }
        if (typeof fetch !== 'undefined') { fetch(RELAY_URL + q, { method: 'GET', cache: 'no-store' }); _markOk(); return; }
        if (typeof XMLHttpRequest !== 'undefined') { var x = new XMLHttpRequest(); x.open('GET', RELAY_URL + q, true); x.send(); _markOk(); return; }
      }
    } catch (e) { _markFail(); }
    // POST 兜底
    var payload = JSON.stringify({ level: level, msg: msg });
    try {
      if (typeof tt !== 'undefined' && tt.request) {
        tt.request({
          url: RELAY_URL, method: 'POST', data: payload,
          header: { 'content-type': 'application/json' },
          success: function () { _markOk(); },
          fail: function () { _markFail(); }
        });
        return;
      }
    } catch (e) { _markFail(); }
    try { if (typeof fetch !== 'undefined') { fetch(RELAY_URL, { method: 'POST', body: payload, keepalive: true }); _markOk(); return; } } catch (e) {}
    try { if (typeof XMLHttpRequest !== 'undefined') { var x = new XMLHttpRequest(); x.open('POST', RELAY_URL, true); x.setRequestHeader('content-type', 'application/json'); x.send(payload); _markOk(); return; } } catch (e) {}
    try { if (typeof navigator !== 'undefined' && navigator.sendBeacon) { navigator.sendBeacon(RELAY_URL, payload); _markOk(); } } catch (e) {}
  }

  function _flush() {
    _timer = null;
    if (!_buf.length) return;
    var batch = _buf.splice(0, _buf.length);
    for (var i = 0; i < batch.length; i++) _sendOne(batch[i].level, batch[i].msg);
  }

  function _schedule() {
    if (_timer || _closed || _dead) return;
    if (typeof setTimeout !== 'function') { _flush(); return; }
    _timer = setTimeout(_flush, 400);
  }

  function _cap(level) {
    var orig = (console && console[level]) ? console[level].bind(console) : function () {};
    console[level] = function () {
      var args = Array.prototype.slice.call(arguments);
      var msg = args.map(function (a) {
        try { return (typeof a === 'object' && a !== null) ? JSON.stringify(a) : String(a); }
        catch (e) { return String(a); }
      }).join(' ');
      _buf.push({ level: level, msg: msg });
      _schedule();
      return orig.apply(null, args);
    };
  }

  ['log', 'info', 'warn', 'error', 'debug'].forEach(_cap);

  function _pushErr(label, e) {
    var stack = (e && (e.stack || e.message)) ? (e.stack || e.message) : String(e);
    _buf.push({ level: 'error', msg: label + ': ' + stack });
    _flush();
  }

  try {
    if (typeof G.addEventListener === 'function') {
      G.addEventListener('error', function (ev) {
        _pushErr('window.error', ev.error || (ev.message + ' @' + (ev.filename || '') + ':' + (ev.lineno || 0)));
      });
      G.addEventListener('unhandledrejection', function (ev) {
        _pushErr('unhandledrejection', ev.reason);
      });
    } else if (typeof G.onerror === 'undefined') {
      G.onerror = function (m, s, l, c, err) { _pushErr('onerror', err || m); };
    }
  } catch (e) {}

  try { if (typeof tt !== 'undefined' && tt.onHide) { tt.onHide(function () { _closed = true; _flush(); }); } } catch (e) {}

  // 初始化信标：加载即发，确认中继通道是否打通（AI 侧能否收到）
  try { console.log('[LogRelay] shim loaded (relay=' + RELAY_URL + ')'); } catch (e) {}
  try { _sendOne('info', '[LogRelay] shim loaded'); } catch (e) {}
})();
/* === END DOUYIN LOG RELAY SHIM === */
