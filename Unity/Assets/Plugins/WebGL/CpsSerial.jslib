mergeInto(LibraryManager.library, {
  $CpsSerial: {
    target: '', generation: 0, operation: 0, button: null, visible: false,
    port: null, reader: null, writer: null, pending: '', discarding: false, lastStatus: '',
    status: function (value) {
      if (CpsSerial.lastStatus === value) return;
      CpsSerial.lastStatus = value;
      SendMessage(CpsSerial.target, 'OnWebSerialStatus', CpsSerial.generation + ':' + value);
    },
    cleanup: function (port, reader, writer) {
      var finishRead = reader ? Promise.resolve(reader.cancel()).catch(function () {}).then(function () { reader.releaseLock(); }) : Promise.resolve();
      var finishWrite = writer ? Promise.resolve(writer.close()).catch(function () {}).then(function () { writer.releaseLock(); }) : Promise.resolve();
      return Promise.all([finishRead, finishWrite]).then(function () { if (port) return port.close(); }).catch(function () {});
    },
    stop: function () {
      CpsSerial.operation++;
      var port = CpsSerial.port, reader = CpsSerial.reader, writer = CpsSerial.writer;
      CpsSerial.port = CpsSerial.reader = CpsSerial.writer = null;
      CpsSerial.pending = ''; CpsSerial.discarding = false;
      return CpsSerial.cleanup(port, reader, writer);
    },
    fail: function (error, operation) {
      if (operation !== CpsSerial.operation) return;
      CpsSerial.status('error:' + (error && error.message ? error.message : String(error)));
      CpsSerial.stop();
    },
    feed: function (text) {
      for (var i = 0; i < text.length; i++) {
        var c = text.charAt(i);
        if (c === '\n') {
          if (!CpsSerial.discarding && CpsSerial.pending.length) {
            SendMessage(CpsSerial.target, 'OnWebSerialChunk', CpsSerial.generation + ':' + CpsSerial.pending.replace(/\r$/, '') + '\n');
          }
          CpsSerial.pending = ''; CpsSerial.discarding = false;
        } else if (!CpsSerial.discarding) {
          if (CpsSerial.pending.length === 1024) { CpsSerial.pending = ''; CpsSerial.discarding = true; }
          else CpsSerial.pending += c;
        }
      }
    },
    pump: function (reader, decoder, operation) {
      reader.read().then(function (result) {
        if (operation !== CpsSerial.operation) return;
        if (result.done) { CpsSerial.fail(new Error('Serial device disconnected.'), operation); return; }
        CpsSerial.feed(decoder.decode(result.value, { stream: true }));
        CpsSerial.pump(reader, decoder, operation);
      }).catch(function (error) { CpsSerial.fail(error, operation); });
    },
    connect: function () {
      if (CpsSerial.port || CpsSerial.lastStatus === 'connecting') return;
      var operation = ++CpsSerial.operation;
      CpsSerial.status('connecting');
      var request;
      try { request = navigator.serial.requestPort(); }
      catch (error) { CpsSerial.fail(error, operation); return; }
      Promise.resolve(request).then(function (port) {
        if (operation !== CpsSerial.operation || !CpsSerial.visible) return;
        return port.open({ baudRate: 115200 }).then(function () {
          if (operation !== CpsSerial.operation || !CpsSerial.visible) { return CpsSerial.cleanup(port, null, null); }
          CpsSerial.port = port;
          CpsSerial.reader = port.readable.getReader();
          CpsSerial.writer = port.writable.getWriter();
          CpsSerial.status('connected');
          CpsSerial.pump(CpsSerial.reader, new TextDecoder(), operation);
        });
      }).catch(function (error) { CpsSerial.fail(error, operation); });
    }
  },
  CpsSerial_Show__deps: ['$CpsSerial'],
  CpsSerial_Show: function (target, generation, x, y, width, height) {
    target = UTF8ToString(target);
    if (generation !== CpsSerial.generation) { CpsSerial.stop(); CpsSerial.lastStatus = ''; }
    CpsSerial.target = target; CpsSerial.generation = generation;
    if (!window.isSecureContext || typeof navigator === 'undefined' || !navigator.serial) {
      CpsSerial.status('unsupported:Use a Web Serial browser on HTTPS or localhost.'); return;
    }
    CpsSerial.visible = true;
    if (!CpsSerial.button) {
      var button = document.createElement('button');
      button.type = 'button'; button.textContent = 'Connect USB';
      button.setAttribute('aria-label', 'Connect USB controller and choose a serial port');
      button.style.position = 'fixed'; button.style.zIndex = '2147483000';
      button.style.background = 'transparent'; button.style.color = 'transparent';
      button.style.border = 'none'; button.style.borderRadius = '14px'; button.style.cursor = 'pointer';
      button.addEventListener('click', function () { CpsSerial.connect(); });
      button.addEventListener('focus', function () { button.style.outline = '3px solid #FA7D30'; });
      button.addEventListener('blur', function () { button.style.outline = ''; });
      document.body.appendChild(button); CpsSerial.button = button;
    }
    var bounds = Module.canvas.getBoundingClientRect();
    CpsSerial.button.style.left = (bounds.left + x * bounds.width) + 'px';
    CpsSerial.button.style.top = (bounds.top + y * bounds.height) + 'px';
    CpsSerial.button.style.width = (width * bounds.width) + 'px';
    CpsSerial.button.style.height = (height * bounds.height) + 'px';
  },
  CpsSerial_Hide__deps: ['$CpsSerial'],
  CpsSerial_Hide: function () {
    CpsSerial.visible = false;
    if (CpsSerial.button) { CpsSerial.button.remove(); CpsSerial.button = null; }
    if (!CpsSerial.port && CpsSerial.lastStatus === 'connecting') {
      CpsSerial.stop(); CpsSerial.status('cancelled');
    }
  },
  CpsSerial_Close__deps: ['$CpsSerial'],
  CpsSerial_Close: function () { CpsSerial.stop(); CpsSerial.status('disconnected'); },
  CpsSerial_Write__deps: ['$CpsSerial'],
  CpsSerial_Write: function (text) {
    var operation = CpsSerial.operation;
    if (!CpsSerial.writer) { CpsSerial.fail(new Error('Serial port is not writable.'), operation); return; }
    try {
      CpsSerial.writer.write(new TextEncoder().encode(UTF8ToString(text))).catch(function (error) { CpsSerial.fail(error, operation); });
    } catch (error) { CpsSerial.fail(error, operation); }
  }
});
