/**
 * Map Module - Simple pan and draw spawn boxes
 * Map is scaled 2.0x for better detail, viewport is 800x600
 */
window.mapModule = {
canvas: null,
ctx: null,
img: null,
    
imageWidth: 0,
imageHeight: 0,
viewportWidth: 800,
viewportHeight: 600,
scale: 2.0, // Map displayed at 2x size
    
// Pan offset (how much the map has moved)
panX: 0,
panY: 0,
    
// Drawing state
isDrawing: false,
drawStartWorld: { x: 0, y: 0 },
drawCurrentWorld: { x: 0, y: 0 },
    
// Panning state
isPanning: false,
panStartScreen: { x: 0, y: 0 },
panStartOffset: { x: 0, y: 0 },
    
// Stored spawns for redrawing
currentSpawns: [],
    
// Settings from C#
boxColor: '#8B0000',
boxLineSize: 2,
boxColorInc: 0.3,
    
    init: function(imgWidth, imgHeight) {
        this.canvas = document.getElementById('mapCanvas');
        this.img = document.getElementById('mapImg');
        
        if (!this.canvas || !this.img) {
            console.error('Canvas or image not found');
            return false;
        }
        
        this.ctx = this.canvas.getContext('2d');
        this.imageWidth = imgWidth;
        this.imageHeight = imgHeight;
        this.panX = 0;
        this.panY = 0;
        this.currentSpawns = [];
        
        console.log(`? Map initialized: ${imgWidth}x${imgHeight} at ${this.scale}x scale, viewport: ${this.viewportWidth}x${this.viewportHeight}`);
        return true;
    },
    
    // Update settings from C#
    updateSettings: function(boxColor, boxLineSize, boxColorInc) {
        this.boxColor = boxColor || '#8B0000';
        this.boxLineSize = boxLineSize || 2;
        this.boxColorInc = boxColorInc || 0.3;
        console.log(`? Settings updated: color=${this.boxColor}, lineSize=${this.boxLineSize}, colorInc=${this.boxColorInc}`);
        
        // Redraw with new settings
        this.redrawAll();
    },
    
    // Calculate color based on priority (brightness adjustment)
    getColorForPriority: function(priority) {
        if (priority === 0) {
            return this.boxColor;
        }
        
        // Parse hex color
        const hex = this.boxColor.replace('#', '');
        let r = parseInt(hex.substr(0, 2), 16);
        let g = parseInt(hex.substr(2, 2), 16);
        let b = parseInt(hex.substr(4, 2), 16);
        
        // Calculate brightness increase (multiply by 255 for more visible effect)
        const brightnessInc = priority * this.boxColorInc * 255;
        
        // Add brightness to each channel and clamp to 255
        r = Math.min(255, Math.round(r + brightnessInc));
        g = Math.min(255, Math.round(g + brightnessInc));
        b = Math.min(255, Math.round(b + brightnessInc));
        
        // Convert back to hex
        const rHex = r.toString(16).padStart(2, '0');
        const gHex = g.toString(16).padStart(2, '0');
        const bHex = b.toString(16).padStart(2, '0');
        
        const resultColor = `#${rHex}${gHex}${bHex}`;
        console.log(`Priority ${priority}: ${this.boxColor} -> ${resultColor} (inc: ${brightnessInc})`);
        
        return resultColor;
    },
    
    // Convert screen pixel to world (map) pixel - accounting for 2x scale
    screenToWorld: function(screenX, screenY) {
        const worldX = Math.round((screenX - this.panX) / this.scale);
        const worldY = Math.round((screenY - this.panY) / this.scale);
        return { x: worldX, y: worldY };
    },
    
    // Convert world (map) pixel to screen pixel - accounting for 2x scale
    worldToScreen: function(worldX, worldY) {
        const screenX = (worldX * this.scale) + this.panX;
        const screenY = (worldY * this.scale) + this.panY;
        return { x: screenX, y: screenY };
    },
    
    // Clamp pan to keep map within viewport
    clampPan: function() {
        const scaledWidth = this.imageWidth * this.scale;
        const scaledHeight = this.imageHeight * this.scale;
        
        // Don't let right edge go past left of viewport
        if (this.panX > 0) this.panX = 0;
        // Don't let bottom edge go past top of viewport
        if (this.panY > 0) this.panY = 0;
        
        // Don't let left edge go past right of viewport
        const minPanX = this.viewportWidth - scaledWidth;
        if (this.panX < minPanX && scaledWidth > this.viewportWidth) {
            this.panX = minPanX;
        }
        
        // Don't let top edge go past bottom of viewport
        const minPanY = this.viewportHeight - scaledHeight;
        if (this.panY < minPanY && scaledHeight > this.viewportHeight) {
            this.panY = minPanY;
        }
    },
    
    startDrawing: function(screenX, screenY) {
        const world = this.screenToWorld(screenX, screenY);
        this.isDrawing = true;
        this.drawStartWorld = { x: world.x, y: world.y };
        this.drawCurrentWorld = { x: world.x, y: world.y };
        console.log(`??? Draw start: world (${world.x}, ${world.y})`);
    },
    
    updateDrawing: function(screenX, screenY) {
        if (!this.isDrawing) return;
        const world = this.screenToWorld(screenX, screenY);
        this.drawCurrentWorld = { x: world.x, y: world.y };
        this.redrawAll(); // Show preview
    },
    
    finishDrawing: function() {
        if (!this.isDrawing) {
            this.isDrawing = false;
            return null;
        }
        
        this.isDrawing = false;
        
        const x1 = Math.min(this.drawStartWorld.x, this.drawCurrentWorld.x);
        const y1 = Math.min(this.drawStartWorld.y, this.drawCurrentWorld.y);
        const x2 = Math.max(this.drawStartWorld.x, this.drawCurrentWorld.x);
        const y2 = Math.max(this.drawStartWorld.y, this.drawCurrentWorld.y);
        
        const width = x2 - x1;
        const height = y2 - y1;
        
        // Minimum 3x3 pixels (since we're at 2x scale, this gives 6x6 screen pixels)
        if (width < 3 || height < 3) {
            console.log('? Box too small, rejected');
            this.redrawAll();
            return null;
        }
        
        const result = {
            X: x1,
            Y: y1,
            Width: width,
            Height: height
        };
        
        console.log(`? Box created: (${x1}, ${y1}) ${width}x${height}`);
        return result;
    },
    
    startPanning: function(screenX, screenY) {
        this.isPanning = true;
        this.panStartScreen = { x: screenX, y: screenY };
        this.panStartOffset = { x: this.panX, y: this.panY };
    },
    
    updatePanning: function(screenX, screenY) {
        if (!this.isPanning) return;
        
        const deltaX = screenX - this.panStartScreen.x;
        const deltaY = screenY - this.panStartScreen.y;
        
        this.panX = this.panStartOffset.x + deltaX;
        this.panY = this.panStartOffset.y + deltaY;
        
        // Keep map within bounds
        this.clampPan();
        
        // Move the image (CSS transform handles the scale, we just adjust position)
        this.img.style.left = this.panX + 'px';
        this.img.style.top = this.panY + 'px';
        
        // Redraw canvas
        this.redrawAll();
    },
    
    finishPanning: function() {
        if (!this.isPanning) {
            this.isPanning = false;
            return null;
        }
        
        this.isPanning = false;
        const result = { X: this.panX, Y: this.panY };
        return result;
    },
    
    applyPan: function(panX, panY) {
        this.panX = panX;
        this.panY = panY;
        this.clampPan();
        
        // Move the image
        this.img.style.left = this.panX + 'px';
        this.img.style.top = this.panY + 'px';
        
        // Redraw canvas
        this.redrawAll();
    },
    
    clearCanvas: function() {
        if (this.ctx && this.canvas) {
            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        }
    },
    
    redraw: function(spawns) {
        console.log(`?? Redraw called with ${spawns ? spawns.length : 0} spawns`);
        
        // Store spawns for future redraws
        this.currentSpawns = spawns || [];
        this.redrawAll();
    },
    
    redrawAll: function() {
        if (!this.ctx || !this.canvas) return;
        
        this.clearCanvas();
        
        // Draw XML spawners first (underneath user spawns)
        if (this.xmlSpawners && Array.isArray(this.xmlSpawners)) {
            this.xmlSpawners.forEach((spawner) => {
                if (!spawner) return;
                
                const x = spawner.x || 0;
                const y = spawner.y || 0;
                const w = spawner.width || 0;
                const h = spawner.height || 0;
                
                // Convert world to screen
                const topLeft = this.worldToScreen(x, y);
                const bottomRight = this.worldToScreen(x + w, y + h);
                
                const screenW = bottomRight.x - topLeft.x;
                const screenH = bottomRight.y - topLeft.y;
                
                // Draw XML spawner box (green, semi-transparent)
                this.ctx.strokeStyle = '#00FF00';
                this.ctx.lineWidth = 1;
                this.ctx.globalAlpha = 0.4;
                this.ctx.strokeRect(topLeft.x, topLeft.y, screenW, screenH);
                
                // Draw "X" marker in center
                const centerX = topLeft.x + screenW / 2;
                const centerY = topLeft.y + screenH / 2;
                const markerSize = 3;
                
                this.ctx.strokeStyle = '#00FF00';
                this.ctx.lineWidth = 2;
                this.ctx.globalAlpha = 0.6;
                this.ctx.beginPath();
                this.ctx.moveTo(centerX - markerSize, centerY - markerSize);
                this.ctx.lineTo(centerX + markerSize, centerY + markerSize);
                this.ctx.moveTo(centerX + markerSize, centerY - markerSize);
                this.ctx.lineTo(centerX - markerSize, centerY + markerSize);
                this.ctx.stroke();
                this.ctx.globalAlpha = 1.0;
            });
        }
        
        // Draw server spawns (heatmap) - colored dots showing where creatures spawned
        if (this.serverSpawns && Array.isArray(this.serverSpawns)) {
            this.serverSpawns.forEach((spawn) => {
                if (!spawn) return;
                
                // Draw player location (larger square, 10x10 pixels)
                const playerPos = this.worldToScreen(spawn.playerX, spawn.playerY);
                const playerColor = `rgb(${spawn.colorR}, ${spawn.colorG}, ${spawn.colorB})`;
                
                this.ctx.fillStyle = playerColor;
                this.ctx.globalAlpha = 0.7;
                this.ctx.fillRect(playerPos.x - 5, playerPos.y - 5, 10, 10);
                
                // Draw spawn location (smaller dot, 2x2 pixels)
                const spawnPos = this.worldToScreen(spawn.spawnX, spawn.spawnY);
                this.ctx.fillStyle = playerColor;
                this.ctx.globalAlpha = 0.9;
                this.ctx.fillRect(spawnPos.x - 1, spawnPos.y - 1, 2, 2);
                
                this.ctx.globalAlpha = 1.0;
            });
        }
        
        // Draw existing spawn boxes
        if (this.currentSpawns && Array.isArray(this.currentSpawns)) {
            this.currentSpawns.forEach((spawn, idx) => {
                if (!spawn || !spawn.spawnBox) {
                    return;
                }
                
                const box = spawn.spawnBox;
                const x = box.x || box.X;
                const y = box.y || box.Y;
                const w = box.width || box.Width;
                const h = box.height || box.Height;
                
                // Convert world to screen (accounting for 2x scale)
                const topLeft = this.worldToScreen(x, y);
                const bottomRight = this.worldToScreen(x + w, y + h);
                
                const screenW = bottomRight.x - topLeft.x;
                const screenH = bottomRight.y - topLeft.y;
                
                // Get priority (default 0 if not set)
                const priority = spawn.spawnPriority || 0;
                
                // Draw box with priority-based color
                this.ctx.strokeStyle = this.getColorForPriority(priority);
                this.ctx.lineWidth = this.boxLineSize;
                this.ctx.strokeRect(topLeft.x, topLeft.y, screenW, screenH);
                
                // Draw position number
                this.ctx.fillStyle = '#FFFFFF';
                this.ctx.font = 'bold 16px Arial';
                this.ctx.textAlign = 'center';
                this.ctx.textBaseline = 'top';
                const pos = spawn.position || (idx + 1);
                this.ctx.fillText(pos.toString(), topLeft.x + screenW / 2, topLeft.y + 5);
            });
        }
        
        // Draw cyan preview while drawing
        if (this.isDrawing) {
            const topLeft = this.worldToScreen(this.drawStartWorld.x, this.drawStartWorld.y);
            const bottomRight = this.worldToScreen(this.drawCurrentWorld.x, this.drawCurrentWorld.y);
            
            const x1 = Math.min(topLeft.x, bottomRight.x);
            const y1 = Math.min(topLeft.y, bottomRight.y);
            const x2 = Math.max(topLeft.x, bottomRight.x);
            const y2 = Math.max(topLeft.y, bottomRight.y);
            
            const w = x2 - x1;
            const h = y2 - y1;
            
            // Draw cyan preview
            this.ctx.strokeStyle = '#00FFFF';
            this.ctx.lineWidth = 3;
            this.ctx.globalAlpha = 0.8;
            this.ctx.strokeRect(x1, y1, w, h);
            this.ctx.globalAlpha = 1.0;
        }
    },
    
    // XML Spawner methods
    showXMLSpawners: function(spawners) {
        console.log(`??? Showing ${spawners.length} XML spawners`);
        this.xmlSpawners = spawners;
        this.redrawAll();
    },
    
    hideXMLSpawners: function() {
        console.log(`??????? Hiding XML spawners`);
        this.xmlSpawners = null;
        this.redrawAll();
    },
    
    // Server Spawn methods (heatmap)
    showServerSpawns: function(spawnData) {
        console.log(`?? Showing ${spawnData.length} server spawn statistics`);
        this.serverSpawns = spawnData;
        this.redrawAll();
    },
    
    hideServerSpawns: function() {
        console.log(`?? Hiding server spawns`);
        this.serverSpawns = null;
        this.redrawAll();
    }
};

console.log('??? Map module loaded');




