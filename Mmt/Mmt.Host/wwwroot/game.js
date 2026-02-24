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

const squareSize = 16;
let currentTileSize = 4;

function rows() { return Math.floor(height / squareSize / currentTileSize); }
function columns() { return Math.floor(width / squareSize / currentTileSize); }
function size() { return squareSize * currentTileSize; }

let rotate = false;
let left = false;
let right = false;
let block = false;
let blockedSince = 0;
let maxBlock = 2000.0;

let blockState = null;
let players = null;
let cleared = 0;

let gameId = window.location.hash?.replace("#", "");
let nextGameId;
let playerId = null;
let playerIndex = null;
let playerName = "Test";
const playerColor = randomColor();
let currentHealth = 40;
let gameStarted = false;
let isDead = false;
let isNr1 = false;
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
        nextGameId = data.nextGameId;

        playerIndex = players?.filter(x => !x.isDead).findIndex(x => x.id == playerId);
        isNr1 = false;

        let player = players?.find(x => x.id == playerId);

        if (player) {
            currentHealth = player.health ?? 1;
            isNr1 = players.filter(x => x.id != playerId).every(x => x.health < currentHealth);

            if (currentHealth <= 0 && !isDead) {
                isDead = player.isDead;
                if (isDead) {
                    playerDead();
                }
            }
        }

        if (gameStarted && !currentBlockCenter) {
            currentBlockCenter = getNewPosition();
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
        window.location.hash = `#${nextGameId}`;
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

let currentBlockCenter = null;
let currentShape = getNewShape();
let currentRotation = 0;

function getNewPosition() {
    let alivePlayers = players.filter(x => !x.isDead).length;
    if (alivePlayers == 0) {
        return [colums() / 2, 0];
    }

    let insert = Math.floor((columns() / alivePlayers) * (playerIndex + 0.5));
    return [insert, 0];
}

function getNewShape() {
    let type = 0;
    if (isNr1) {
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
        event.preventDefault();
        if (!block) {
            blockedSince = oldInputTimestamp;
            block = true;
        }
    }
    if (event.code === "Space") {
        event.preventDefault();
        if (!block) {
            smashDown();
        }
    }
}

window.onkeyup = (event) => {
    if (!gameStarted || isDead) {
        return;
    }

    if (event.code === "ArrowUp") {
        event.preventDefault();
        let newRotation = (currentRotation + 1) % 4;
        let [_, hasCollision] = willCollide(currentBlockCenter, newRotation, xy => xy);
        if (!hasCollision) {
            currentRotation = newRotation;
        }
    }
    if (event.code === "ArrowLeft") {
        event.preventDefault();
        left = false;
    }
    if (event.code === "ArrowRight") {
        event.preventDefault();
        right = false;
    }
    if (event.code === "ArrowDown") {
        event.preventDefault();
        block = false;
    }
}

function randomColor() {
    let parts = shuffleArray(["f", "d", "b", "8", "6", "4"]);
    return "#" + parts[0] + parts[1] + parts[2];
}

function shuffleArray(array) {
    for (let i = array.length - 1; i > 0; i--) {
        let j = Math.floor(Math.random() * (i + 1));
        let temp = array[i];
        array[i] = array[j];
        array[j] = temp;
    }
    return array;
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
        return [null, false];
    }

    let currentBlock = getBlockPositions(position, rotation);
    let collidingBlocks = currentBlock.map(mutation);

    if (position[1] < 2) {
        return [currentBlock, collidingBlocks.some(([x, y]) => x < 0 || x >= columns())];
    }

    let hasCollision =
        collidingBlocks.some(([x, y]) => x < 0 || x >= columns() || y < 0 || y >= rows()) ||
        collidingBlocks.some(([x, y]) => blockState[y][x].color != null);

    return [currentBlock, hasCollision];
}

function handleState() {
    if (!block && blockState) {
        let [currentBlock, hasCollision] = willCollide(currentBlockCenter, currentRotation, ([x, y]) => [x, y + 1]);

        if (hasCollision) {
            placeBlock(currentBlock, currentBlockCenter);
        }
        else {
            moveBlock();
        }
    }
    else if (block && blockState) {
        if (oldInputTimestamp - blockedSince > maxBlock) {
            let currentBlock = getBlockPositions(currentBlockCenter, currentRotation);
            placeBlock(currentBlock, currentBlockCenter);
            block = false;
        }
    }
}

function smashDown() {
    let bottomRow = rows();
    for (let r = bottomRow - 1; r > currentBlockCenter[1]; r--) {
        let [positions, hasCollision] = willCollide([currentBlockCenter[0], r], currentRotation, ([x, y]) => [x, y]);

        let allInScreen = positions.every(([x, y]) => y < bottomRow);

        if (!hasCollision && allInScreen) {
            placeBlock(positions, [currentBlockCenter[0], r]);
            break;
        }
    }
}

function placeBlock(placedBlock, placedBlockCenter) {
    for (let [x, y] of placedBlock) {
        if (x >= 0 && x < columns() && y >= 0 && y < rows()) {
            blockState[y][x].color = playerColor;
        }
    }

    let json = JSON.stringify({
        currentBlock: placedBlock,
        centerPosition: placedBlockCenter,
        blockPlaced: true
    });
    ws.send(json);

    currentBlockCenter = getNewPosition();
    currentShape = getNewShape();
    currentRotation = Math.floor(Math.random() * 4);
}

function moveBlock() {
    currentBlockCenter[1]++;

    let json = JSON.stringify({
        currentBlock: getBlockPositions(currentBlockCenter, currentRotation),
        centerPosition: currentBlockCenter,
        blockPlaced: false
    });
    ws.send(json);
}

function drawFrame() {
    if (blockState && players) {
        drawBackground();

        drawState();

        if (currentBlockCenter) {
            let currentBlock = getBlockPositions(currentBlockCenter, currentRotation);
            let percentage = block ? ((oldInputTimestamp - blockedSince) / maxBlock) : null;

            for (let [x, y] of currentBlock) {
                drawBlock(y, x, { color: playerColor, isActive: true }, percentage);
            }

            for (let player of players) {
                if (player.centerPosition && player.id != playerId) {
                    drawPlayer(player.centerPosition.x, player.centerPosition.y, player.name);
                }
            }
        }

        let alivePlayers = players.filter(p => !p.isDead);
        inserts.innerHTML = alivePlayers.map((p, i) => `<div style="--color: ${p.color};left: ${offset(alivePlayers.length, i)}px"></div>`).join("");
        health.innerHTML = players.map((p) => `<div style="--color: ${p.color}">${p.name} ${p.health} ${(p.isDead ? "(dead)" : "")} ${(p.ready ? "" : "(not ready)")}</div>`).join("");
    }
}

let oldInputTimestamp = 0;
let oldStateTimestamp = 0;

let speed = 10;

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

function offset(playerCount, index) {
    let playerWidth = width / playerCount;

    return Math.round(playerWidth * (index + 0.5));
}

joinButton.onclick = initGame;
readyButton.onclick = joinGame;
