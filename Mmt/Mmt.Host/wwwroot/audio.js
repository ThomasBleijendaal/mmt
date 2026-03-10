class AudioManager {
    static {
    }

    static audioInitialized = false;

    static blockedSound;
    static downSound;
    static loseSound;
    static moveSound;
    static placeSound;
    static rotateSound;
    static weirdBlockSound;
    static winSound;

    static init = function () {
        if (this.audioInitialized) {
            return;
        }

        this.audioInitialized = true;
        this.bufferSounds();
    }

    static preloadFile = async function (file) {
        let response = await fetch(file);
        let arrayBuffer = await response.arrayBuffer();
        let audioBuffer = await this.audioCtx.decodeAudioData(arrayBuffer);
        return audioBuffer;
    }

    static bufferSounds = async function () {
        this.audioCtx = new AudioContext();

        this.blockedSound = await this.preloadFile("/audio/blocked.mp3");
        this.downSound = await this.preloadFile("/audio/down.mp3");
        this.loseSound = await this.preloadFile("/audio/lose.mp3");
        this.moveSound = await this.preloadFile("/audio/move.mp3");
        this.placeSound = await this.preloadFile("/audio/place.mp3");
        this.rotateSound = await this.preloadFile("/audio/rotate.mp3");
        this.weirdBlockSound = await this.preloadFile("/audio/weird-block.mp3");
        this.winSound = await this.preloadFile("/audio/win.mp3");
    }

    static playSound = function (audioBuffer, randomize, offset, length, pitch) {
        if (this.audioCtx == null) {
            return;
        }

        const trackSource = this.audioCtx.createBufferSource();
        const gain = this.audioCtx.createGain();
        trackSource.buffer = audioBuffer;
        trackSource.connect(gain);
        gain.connect(this.audioCtx.destination);

        if (randomize) {
            trackSource.detune.value = -1200 * Math.random();
        }
        if (pitch) {
            trackSource.detune.value = -400 + (400 * pitch);
        }

        if (offset && length) {
            trackSource.start(0, offset, length);
        }
        else if (length) {
            trackSource.start(0, 0, length);
        }
        else if (offset) {
            trackSource.start(0, offset);
        }
        else {
            trackSource.start();
        }
    }

    static playBlockedSound = function (pitch) {
        this.playSound(this.blockedSound, false, 0.01, 0.2, pitch);
    }

    static playDownSound = function () {
        this.playSound(this.downSound);
    }

    static playLoseSound = function () {
        this.playSound(this.loseSound);
    }

    static playMoveSound = function () {
        this.playSound(this.moveSound, true, 0.02);
    }

    static playPlaceSound = function () {
        this.playSound(this.placeSound, false, 0.04);
    }

    static playRotateSound = function () {
        this.playSound(this.rotateSound, true, 0.05);
    }

    static playWeirdBlockSound = function () {
        this.playSound(this.weirdBlockSound);
    }

    static playWinSound = function () {
        this.playSound(this.winSound);
    }
}