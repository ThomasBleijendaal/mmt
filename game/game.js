const canvas = document.querySelector("#game");
const health = document.querySelector("#health");
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

let blockState = new Array(rows);

for (let r = 0; r < rows; r++) {
    blockState[r] = new Array(columns);
    
    for (let c = 0; c < columns; c++) {
        blockState[r][c] = { isEmpty: true, color: null, id: `${c} x ${r}` };
        
        if (r > rows / 2 && Math.random() > 0.1) {
            blockState[r][c].isEmpty = false;
            blockState[r][c].color = Math.random() > 0.7 ? 'red' : 'blue';
        }
    }
}

const playerColor = randomColor();

let playerCount = 3;
let playerHealth = { 'red' : 50, 'blue': 50 };
playerHealth[playerColor] = 50;

let blockTypes = [
    // xx
    // xx
    [[0, 0], [0, 1], [1, 1], [1, 0]],

    // xxx
    //   x
    [[-1, 0], [0, 0], [0, 1], [1, 1]],

    // xx
    //  xx
    [[-1, 0], [0, 0], [1, 0], [1, 1]],

    // xxxx
    //
    [[-1, 0], [0, 0], [1, 0], [2, 0]],

    // xxx
    //  x
    [[-1, 0], [0, 0], [1, 0], [0, 1]]
];

let currentBlockCenter = getNewPosition();
let currentShape = getNewShape();
let currentRotation = 0;

function getNewPosition() {
    return [2 + Math.floor(Math.random() * (columns - 4)), 0];
}
function getNewShape() {
    return Math.floor(Math.random() * 5);
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
    function randomPart() { return ["4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e"][Math.floor(Math.random() * 11)];}
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
    if (!block) {
        let [currentBlock, hasCollision] = willCollide(currentBlockCenter, currentRotation, ([x, y]) => [x, y + 1]);

        if (hasCollision) {
            for (var [x, y] of currentBlock) {
                if (x >= 0 && x < columns && y >= 0 && y < rows) {
                    blockState[y][x].isEmpty = false;
                    blockState[y][x].color = playerColor;
                }
            }

            // TODO: Emit blocks

            currentBlockCenter = getNewPosition();
            currentShape = getNewShape();
            currentRotation = Math.floor(Math.random() * 4);
        }
        else {
            currentBlockCenter[1]++;
        }
    }

    for (let r = rows - 1; r >= 0; r--) {
        if (blockState[r].every(b => !b.isEmpty)) {
            let rowsComplete = 1;
            while (rowsComplete <= r && blockState[r - rowsComplete].every(b => !b.isEmpty)) {
                rowsComplete++;
            }

            let blocks = blockState.slice(r - rowsComplete + 1, r + 1).flat();
            let totalBlocks = blocks.length;

            let groupings = Object.entries(
                Object.groupBy(blocks, ({color}) => color))
                .map(([color, groupBlocks]) => ({ color: color, percentage: (groupBlocks.length / totalBlocks) }));
            
            groupings.sort(({percentage}) => percentage);
            groupings.map(obj => obj.dHealth = Math.floor((-10 / playerCount) + (10 * obj.percentage)));

            for (const { color, percentage, dHealth } of groupings) {
                console.log(color, percentage, dHealth);

                playerHealth[color] += dHealth;
            }

            for (let [color, _] of Object.entries(playerHealth)) {
                if (!groupings.some(g => g.color === color)) {
                    playerHealth[color] -= 10;
                }
            }
            
            for (let nr = r; nr >= rowsComplete; nr--) {
                blockState[nr - rowsComplete].forEach((b, c) => {
                    blockState[nr][c].isEmpty = b.isEmpty;
                    blockState[nr][c].color = b.color;
                });
            }

            for (let nr = 0; nr <= rowsComplete; nr++) {
                blockState[nr].forEach(b => {
                    b.isEmpty = true;
                    b.color = null;
                });
            }



            // TODO: Emit event
        }
    }
}

function drawFrame() {
    drawBackground();

    drawState();

    let currentBlock = getBlockPositions(currentBlockCenter, currentRotation);
    for (var [x, y] of currentBlock) {
        drawBlock(x, y, { color: playerColor, isActive: true })
    }

    health.innerHTML = Object.entries(playerHealth).map(([color, health]) => `<div style="--color: ${color}">${health}</div>`).join("");

    // TODO: Emit current blocks
}

let oldInputTimestamp = 0;
let oldStateTimestamp = 0;

let speed = 12;

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

window.requestAnimationFrame(gameLoop);