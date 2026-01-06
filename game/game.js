const canvas = document.querySelector("#game");
const health = document.querySelector("#health");
const rowsCleared = document.querySelector("#rowsCleared");
const ctx = canvas.getContext("2d");

const width = canvas.width;
const height = canvas.height;

const squareSize = 30;

const rows = Math.floor(height / squareSize);
const columns = Math.floor(width / squareSize);

let rotate = false;
let left = false;
let right = false;
let block = false;

let blockState = null;
let players = null;
let cleared = 0;

let playerId = null;
const playerName = "Test"; // prompt("Player name?");
const playerColor = randomColor();
let currentHealth = 40;

/**
 * @type WebSocket
 */
let ws;
 
async function initGame() {

    const joinResponse = await fetch("http://localhost:5021/join", {
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

    playerId = await joinResponse.json();

    ws = new WebSocket(`http://localhost:5021/ws/${playerId}`);
    ws.addEventListener("message", (event) => {
        let data = JSON.parse(event.data);
        players = data.players;
        blockState = data.blockState;
        cleared = data.rowsCleared;
        currentHealth = players?.find(x => x.id == playerId)?.health ?? 1;

        if (currentHealth <= 0) {
            alert("You died!");
            window.location.reload();
        }
    });

    ws.addEventListener("close", (event) => {
        alert("Server crashed!");
        window.location.reload();
    });
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

let currentBlockCenter = getNewPosition();
let currentShape = getNewShape();
let currentRotation = 0;

function getNewPosition() {
    return [2 + Math.floor(Math.random() * (columns - 6)), 0];
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
        return [currentBlock, collidingBlocks.some(([x, y]) => x < 0 || x >= columns)];
    }

    let hasCollision =
        collidingBlocks.some(([x, y]) => x < 0 || x >= columns || y < 0 || y >= rows) ||
        collidingBlocks.some(([x, y]) => !blockState[y][x].isEmpty);

    return [currentBlock, hasCollision];
}

function handleState() {
    if (!block && blockState) {
        let [currentBlock, hasCollision] = willCollide(currentBlockCenter, currentRotation, ([x, y]) => [x, y + 1]);

        if (hasCollision) {
            for (var [x, y] of currentBlock) {
                if (x >= 0 && x < columns && y >= 0 && y < rows) {
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
            drawBlock(x, y, { color: playerColor, isActive: true })
        }

        health.innerHTML = players.map((p) => `<div style="--color: ${p.color}">${p.name} ${p.health}</div>`).join("");
        rowsCleared.innerHTML = cleared;
    }
}

let oldInputTimestamp = 0;
let oldStateTimestamp = 0;

let speed = 12;

async function startGame() {
    await initGame();
    window.requestAnimationFrame(gameLoop);
}

function gameLoop(timeStamp) {
    if (timeStamp - oldInputTimestamp > (1000 / speed)) {
        oldInputTimestamp = timeStamp;

        handleInputs();
    }

    if (timeStamp - oldStateTimestamp > (3000 / speed)) {
        oldStateTimestamp = timeStamp;

        handleState();
    }

    drawFrame();

    window.requestAnimationFrame(gameLoop);
}

startGame();
