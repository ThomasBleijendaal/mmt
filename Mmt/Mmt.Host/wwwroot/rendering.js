function drawBackground() {
    let squareSize = size();

    ctx.fillStyle = "#000000";
    ctx.beginPath();
    ctx.clearRect(0, 0, width, height);

    for (let r = 0; r < rows() + 1; r++) {
        ctx.beginPath();
        ctx.fillStyle = "#444444";
        ctx.rect(0, r * squareSize, width, 1);
        ctx.fill();
    }

    for (let c = 0; c < columns() + 1; c++) {
        ctx.beginPath();
        ctx.fillStyle = "#444444";
        ctx.rect(c * squareSize, 0, 1, height);
        ctx.fill();
    }
}

function drawState() {
    for (let r = 0; r < rows(); r++) {
        for (let c = 0; c < columns(); c++) {
            let block = blockState[r][c];
            if (block.color != null) {
                drawBlock(r, c, block, null, null);
            }
        }
    }
}

function drawBlock(r, c, block, blockPercentage) {
    let squareSize = size();

    ctx.beginPath();
    ctx.fillStyle = block.color;
    ctx.rect(c * squareSize + 1, r * squareSize + 1, squareSize - 1, squareSize - 1);
    ctx.fill();

    if (blockPercentage > 0) {
        ctx.beginPath();
        ctx.globalAlpha = blockPercentage;
        ctx.fillStyle = "#ff0000";
        ctx.rect(c * squareSize + 1, r * squareSize + 1, squareSize - 1, squareSize - 1);
        ctx.fill();

        ctx.beginPath();
        ctx.globalAlpha = 1;
        ctx.fillStyle = block.color;
        ctx.rect(c * squareSize + 3, r * squareSize + 3, squareSize - 5, squareSize - 5);
        ctx.fill();
    }

    ctx.beginPath();
    ctx.globalAlpha = block.isActive ? .8 : .3;
    ctx.fillStyle = "#ffffff";
    ctx.rect(c * squareSize + 3, r * squareSize + 3, squareSize - 5, squareSize - 5);
    ctx.fill();
    
    ctx.globalAlpha = 1;
}

function drawPlayer(r, c, name) {
    let squareSize = size();

    let x = (r * squareSize) + (squareSize / 2);
    let y = (c * squareSize) + (squareSize / 2);

    ctx.font = "20px sans-serif";
    ctx.fillStyle = "#000000";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(name, x, y);

    ctx.font = "20px sans-serif";
    ctx.fillStyle = "#ffffff";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(name, x - 1, y - 1);
}
