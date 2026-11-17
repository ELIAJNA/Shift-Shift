// Shift+Shift: reads the two Shift keys straight from the browser (day 17).
// Why: on Windows, when both Shift keys are down and one is let go, the browser gets no key-up for it. Unity's own
// keyboard state then shows that key as held for good, and its next press is lost. Here every Shift key-up checks
// whether ANY Shift is still down (event.shiftKey); if none is, both are cleared.
// A fresh key-down for a key still marked as down means its key-up was one of the lost ones: reported as "again".
// State bits: 1 left down, 2 right down, 4 left pressed since the last read, 8 right pressed since the last read,
// 16 left pressed again while marked down, 32 right pressed again while marked down,
// 64 / 128 left / right pressed again while the OTHER Shift is held, flagged as a repeat (see "repress" below).
mergeInto(LibraryManager.library, {
  ShiftKeys_Init: function () {
    if (Module.shiftKeys) return;
    var s = Module.shiftKeys = { left: false, right: false, leftHit: false, rightHit: false, leftAgain: false, rightAgain: false,
      leftRepress: false, rightRepress: false, leftLast: 0, rightLast: 0 };
    window.addEventListener('keydown', function (e) {
      // "repeat" cannot be trusted on its own: with one Shift held, Windows flags the FIRST press of the other
      // Shift as a repeat too (day 17: every both-press lost its second key). It only means auto-repeat when
      // this very key is already marked down.
      var isLeft = e.code === 'ShiftLeft', isRight = e.code === 'ShiftRight';
      if (!isLeft && !isRight) return;
      var marked = isLeft ? s.left : s.right;
      var otherMarked = isLeft ? s.right : s.left;
      var now = performance.now();
      var gap = now - (isLeft ? s.leftLast : s.rightLast);
      if (isLeft) s.leftLast = now; else s.rightLast = now;
      if (e.repeat && marked) {
        // Day 25 (tester): with one Shift held, the other one's key-up never arrives, and Windows flags its NEXT
        // press as a repeat too, so a second lane move during a sprint was thrown away as auto-repeat. Auto-repeat
        // events come every ~30-50 ms; a gap longer than that while the other Shift is held is a new press.
        // ShiftInput only uses it if the previous press of this key was already counted as a tap.
        if (otherMarked && gap > 100) { if (isLeft) s.leftRepress = true; else s.rightRepress = true; }
        return;
      }
      if (isLeft) { if (marked) s.leftAgain = true; s.left = true; s.leftHit = true; }
      else { if (marked) s.rightAgain = true; s.right = true; s.rightHit = true; }
    }, true);
    window.addEventListener('keyup', function (e) {
      if (e.code === 'ShiftLeft') s.left = false;
      else if (e.code === 'ShiftRight') s.right = false;
      if (e.key === 'Shift' && !e.shiftKey) { s.left = false; s.right = false; }
    }, true);
    window.addEventListener('blur', function () { s.left = false; s.right = false; });
  },
  // Browsers start sound only after a user gesture, and Chrome does not count Shift (or any modifier key) as one
  // (day 20: the music was silent in Chrome; console: "The AudioContext was not allowed to start").
  // A click, a tap, or a key other than a modifier resumes Unity's audio context.
  AudioUnlock_Init: function () {
    if (Module.audioUnlock) return;
    var u = Module.audioUnlock = { gesture: false };
    function ctx() {
      try { if (typeof WEBAudio !== 'undefined' && WEBAudio && WEBAudio.audioContext) return WEBAudio.audioContext; } catch (e) {}
      return null;
    }
    u.ctx = ctx;
    function unlock(e) {
      if (e.type === 'keydown' && (e.key === 'Shift' || e.key === 'Control' || e.key === 'Alt' || e.key === 'Meta')) return;
      u.gesture = true;
      var c = ctx();
      if (c && c.state !== 'running' && c.resume) { try { c.resume(); } catch (err) {} }
    }
    window.addEventListener('pointerdown', unlock, true);
    window.addEventListener('touchend', unlock, true);
    window.addEventListener('keydown', unlock, true);
  },
  // 1 once sound can play: Unity's audio context is running (or, if it cannot be found, once a gesture happened).
  AudioUnlock_IsRunning: function () {
    var u = Module.audioUnlock;
    if (!u) return 0;
    var c = u.ctx();
    if (c) return c.state === 'running' ? 1 : 0;
    return u.gesture ? 1 : 0;
  },
  ShiftKeys_State: function () {
    var s = Module.shiftKeys;
    if (!s) return 0;
    var bits = (s.left ? 1 : 0) | (s.right ? 2 : 0) | (s.leftHit ? 4 : 0) | (s.rightHit ? 8 : 0) |
      (s.leftAgain ? 16 : 0) | (s.rightAgain ? 32 : 0) | (s.leftRepress ? 64 : 0) | (s.rightRepress ? 128 : 0);
    s.leftHit = s.rightHit = s.leftAgain = s.rightAgain = s.leftRepress = s.rightRepress = false;
    return bits;
  }
});
