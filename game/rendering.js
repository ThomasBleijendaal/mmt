function drawBackground() {
    ctx.beginPath();
    ctx.clearRect(0, 0, width, height);

    for (let r = 0; r < rows + 1; r++) {
        ctx.beginPath();
        ctx.fillStyle = "#444444";
        ctx.rect(0, r * squareSize, width, 1);
        ctx.fill();
    }

    for (let c = 0; c < columns + 1; c++) {
        ctx.beginPath();
        ctx.fillStyle = "#444444";
        ctx.rect(c * squareSize, 0, 1, height);
        ctx.fill();
    }
}

function drawState() {
    for (let r = 0; r < rows; r++) {
        for (let c = 0; c < columns; c++) {
            let block = blockState[r][c];
            if (!block.isEmpty) {
                drawBlock(c, r, block);
            }
        }
    }
}

function drawBlock(r, c, block) {
    ctx.beginPath();
    ctx.fillStyle = block.color;
    ctx.rect(r * squareSize + 1, c * squareSize + 1, squareSize - 1, squareSize - 1);
    ctx.fill();

    ctx.beginPath();
    ctx.globalAlpha = block.isActive ? .8 : .3;
    ctx.fillStyle = "#ffffff";
    ctx.rect(r * squareSize + 3, c * squareSize + 3, squareSize - 5, squareSize - 5);
    ctx.fill();

    ctx.globalAlpha = 1;
}
