const initScreen = document.querySelector("#init-screen");
const joinScreen = document.querySelector("#join-screen");
const gameScreen = document.querySelector("#game-screen");
const deathScreen = document.querySelector("#death-screen");
const winScreen = document.querySelector("#win-screen");
const shareUrl = document.querySelector("#share-url");

const nameInput = document.querySelector("#name");
const joinButton = document.querySelector("#join");
const readyButton = document.querySelector("#ready");

const canvas = document.querySelector("#game");
const health = document.querySelector("#health");
const ctx = canvas.getContext("2d");

const width = canvas.width;
const height = canvas.height;

const squareSize = 20;
let currentTileSize = 4;

function rows() { return Math.floor(height / squareSize / currentTileSize); }
function columns() { return Math.floor(width / squareSize / currentTileSize); }
function size() { return squareSize * currentTileSize; }

let rotate = false;
let left = false;
let right = false;
let block = false;

let blockState = null;
let players = null;
let cleared = 0;

let gameId = window.location.hash?.replace("#", "");
let playerId = null;
let playerName = "Test";
const playerColor = randomColor();
let currentHealth = 40;
let gameStarted = false;
let isDead = false;
let gameFinished = false;

/**
 * @type WebSocket
 */
let ws;

let url = window.location.href.replace(window.location.hash, "");

async function initGame() {
    initScreen.classList.add("hidden");
    joinScreen.classList.remove("hidden");
    gameScreen.classList.remove("hidden");

    playerName = nameInput.value ?? "Dummy";

    const joinResponse = await fetch(`${url}join?gameId=${gameId}`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ name: playerName, color: playerColor })
    });

    if (joinResponse.status == 400) {
        alert("Failed to join: " + await joinResponse.text());
        window.location.reload();
    }

    let result = await joinResponse.json();
    gameId = result.gameId;
    playerId = result.playerId;

    window.location.hash = gameId;
    shareUrl.innerHTML = window.location.href;

    ws = new WebSocket(`${url}ws/${gameId}/${playerId}`);
    ws.addEventListener("message", (event) => {
        let data = JSON.parse(event.data);
        players = data.players;
        blockState = data.blockState;
        cleared = data.rowsCleared;
        currentTileSize = data.tileSize;
        gameStarted = !gameFinished && (data.status == "Running" || data.status == "Finished");

        let player = players?.find(x => x.id == playerId);

        if (player) {
            currentHealth = player?.health ?? 1;

            if (currentHealth <= 0 && !isDead) {
                isDead = player.isDead;
                if (isDead) {
                    playerDead();
                }
            }
        }

        if (data.status == "Finished" && !gameFinished) {
            gameFinished = true;

            if (isDead) {
                restartGame();
            }
            else {
                playerWon();
            }
        }
    });

    ws.addEventListener("close", (event) => {
        alert("Server crashed!");
        window.location.reload();
    });

    window.requestAnimationFrame(gameLoop);
}

function joinGame() {
    joinScreen.classList.add("hidden");

    currentBlockCenter = getNewPosition();
    currentShape = getNewShape();
    currentRotation = 0;

    let json = JSON.stringify({ ready: true });
    ws.send(json);
}

function playerDead() {
    deathScreen.classList.remove("hidden");
    currentBlockCenter = [];
}

function playerWon() {
    winScreen.classList.remove("hidden");
    currentBlockCenter = [];

    restartGame();
}

function restartGame() {
    window.setTimeout(function () {
        alert("Click OK to restart");
        window.location.reload();
    }, 2000);
}

let blockTypes = [
    // Xx
    // xx
    [[0, 0], [0, 1], [1, 1], [1, 0]],

    // xXx
    //   x
    [[-1, 0], [0, 0], [1, 0], [1, 1]],

    //   x
    // xXx
    [[-1, 0], [0, 0], [1, 0], [1, -1]],

    // xX
    //  xx
    [[-1, 0], [0, 0], [1, 0], [1, 1]],

    //  xx
    // xX
    [[-1, 0], [0, 0], [0, 1], [-1, 1]],

    // xXxx
    //
    [[-1, 0], [0, 0], [1, 0], [2, 0]],

    // xXx
    //  x
    [[-1, 0], [0, 0], [1, 0], [0, 1]],

    // x x
    // 
    // x x
    [[-1, -1], [1, -1], [-1, 1], [1, 1]],

    // x
    [[0, 0]],

    // x x
    //  x
    [[-1, 0], [1, 0], [0, 1]],

    // x 
    // x x
    //   x
    [[-1, -1], [-1, 0], [1, 0], [1, 1]],
];
const saneBlocks = 6
const insaneBlocks = 10;

let currentBlockCenter = [];
let currentShape = getNewShape();
let currentRotation = 0;

function getNewPosition() {
    return [2 + Math.floor(Math.random() * (columns() - 3)), 0];
}

function getNewShape() {
    let type = 0;
    if (currentHealth > 80) {
        type = Math.floor(Math.random() * insaneBlocks);
    }
    else {
        type = Math.floor(Math.random() * saneBlocks);
    }
    return type;
}

function getBlockPositions(block, rotation) {
    let blockType = blockTypes[currentShape];
    let rotator =
        rotation === 0 ? ([x, y]) => [x, y] :
            rotation === 1 ? ([x, y]) => [- y, x] :
                rotation === 2 ? ([x, y]) => [- x, - y] :
                    rotation === 3 ? ([x, y]) => [y, - x] :
                        ([x, y]) => [x, y];

    let positions = blockType.map(rotator).map(([x, y]) => [block[0] + x, block[1] + y]);
    return positions;
}

window.onkeydown = (event) => {
    if (!gameStarted || isDead) {
        return;
    }

    if (event.code === "ArrowLeft") {
        event.preventDefault();

        if (!left) {
            oldInputTimestamp = 0;
        }

        left = true;
    }
    if (event.code === "ArrowRight") {
        event.preventDefault();

        if (!right) {
            oldInputTimestamp = 0;
        }

        right = true;
    }
    if (event.code === "ArrowDown") {
        block = true;
        event.preventDefault();
    }
}

window.onkeyup = (event) => {
    if (!gameStarted || isDead) {
        return;
    }

    if (event.code === "ArrowUp") {
        let newRotation = (currentRotation + 1) % 4;
        let [_, hasCollision] = willCollide(currentBlockCenter, newRotation, xy => xy);
        if (!hasCollision) {
            currentRotation = newRotation;
        }

        event.preventDefault();
    }
    if (event.code === "ArrowLeft") {
        left = false;
        event.preventDefault();
    }
    if (event.code === "ArrowRight") {
        right = false;
        event.preventDefault();
    }
    if (event.code === "ArrowDown") {
        block = false;
        event.preventDefault();
    }
}

function randomColor() {
    function randomPart() { return ["4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e"][Math.floor(Math.random() * 11)]; }
    return "#" + randomPart() + randomPart() + randomPart() + randomPart() + randomPart() + randomPart();
}

function handleInputs() {
    if (left && !block) {
        let [_, hasCollision] = willCollide(currentBlockCenter, currentRotation, ([x, y]) => [x - 1, y]);
        if (!hasCollision) {
            currentBlockCenter[0]--;
        }
    }
    if (right && !block) {
        let [_, hasCollision] = willCollide(currentBlockCenter, currentRotation, ([x, y]) => [x + 1, y]);
        if (!hasCollision) {
            currentBlockCenter[0]++;
        }
    }
}

function willCollide(position, rotation, mutation) {
    if (blockState == null) {
        return [currentBlock, false];
    }

    let currentBlock = getBlockPositions(position, rotation);
    let collidingBlocks = currentBlock.map(mutation);

    if (position[1] < 2) {
        return [currentBlock, collidingBlocks.some(([x, y]) => x < 0 || x >= columns())];
    }

    let hasCollision =
        collidingBlocks.some(([x, y]) => x < 0 || x >= columns() || y < 0 || y >= rows()) ||
        collidingBlocks.some(([x, y]) => !blockState[y][x].isEmpty);

    return [currentBlock, hasCollision];
}

function handleState() {
    if (!block && blockState) {
        let [currentBlock, hasCollision] = willCollide(currentBlockCenter, currentRotation, ([x, y]) => [x, y + 1]);

        if (hasCollision) {
            for (var [x, y] of currentBlock) {
                if (x >= 0 && x < columns() && y >= 0 && y < rows()) {
                    blockState[y][x].isEmpty = false;
                    blockState[y][x].color = playerColor;
                }
            }

            let json = JSON.stringify({
                currentBlock: getBlockPositions(currentBlockCenter, currentRotation),
                blockPlaced: true
            });
            ws.send(json);

            currentBlockCenter = getNewPosition();
            currentShape = getNewShape();
            currentRotation = Math.floor(Math.random() * 4);
        }
        else {
            currentBlockCenter[1]++;

            let json = JSON.stringify({
                currentBlock: getBlockPositions(currentBlockCenter, currentRotation),
                blockPlaced: false
            });
            ws.send(json);
        }
    }
}

function drawFrame() {
    if (blockState && players) {
        drawBackground();

        drawState();

        let currentBlock = getBlockPositions(currentBlockCenter, currentRotation);
        for (var [x, y] of currentBlock) {
            drawBlock(x, y, { color: playerColor, isActive: true });
        }

        health.innerHTML = players.map((p) => `<div style="--color: ${p.color}">${p.name} ${p.health} ${(p.isDead ? "(dead)" : "")} ${(p.ready ? "" : "(not ready)")}</div>`).join("");
    }
}

let oldInputTimestamp = 0;
let oldStateTimestamp = 0;

let speed = 12;

function gameLoop(timeStamp) {
    if (gameStarted && !isDead) {
        if (timeStamp - oldInputTimestamp > (1000 / speed)) {
            oldInputTimestamp = timeStamp;

            handleInputs();
        }

        if (timeStamp - oldStateTimestamp > (3000 / speed)) {
            oldStateTimestamp = timeStamp;

            handleState();
        }
    }

    drawFrame();

    window.requestAnimationFrame(gameLoop);
}

joinButton.onclick = initGame;
readyButton.onclick = joinGame;
