/**
 * Map Module - Simple pan and draw spawn boxes
 * Map supports 1x (actual size) and 2x (zoomed) viewing
 */
window.mapModule = {
canvas: null,
ctx: null,
img: null,
    
imageWidth: 0,
imageHeight: 0,
viewportWidth: 800,
viewportHeight: 600,
scale: 2.0, // Default 2x zoom, can be changed to 1.0 for actual size
    
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

// Region data for region spawn page
regions: null,
selectedRegionName: '',
hoveredRegionName: '',

// Settings from C#
boxColor: '#8B0000',
boxLineSize: 2,
boxColorInc: 0.3,
    
// Keyboard panning state
keyStates: new Set(),
panAnimationId: null,
    
    init: function(imgWidth, imgHeight, canvasId, imgId) {
        this.canvas = document.getElementById(canvasId || 'mapCanvas');
        this.img = document.getElementById(imgId || 'mapImg');

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
        this.regions = null;
        this.selectedRegionName = '';
        this.hoveredRegionName = '';

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
    
    // Set zoom level (1.0 = actual size, 2.0 = 2x zoomed)
    setZoomLevel: function(zoomLevel) {
        console.log(`? Zoom level changed from ${this.scale}x to ${zoomLevel}x`);
        this.scale = zoomLevel;
        
        // Redraw everything at new scale
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

        // Draw regions first (underneath everything)
        if (this.regions && Array.isArray(this.regions)) {
            this.regions.forEach((region) => {
                if (!region) return;

                const isSelected = region.name === this.selectedRegionName;
                const isHovered = region.name === this.hoveredRegionName;

                let fillColor, strokeColor, lineWidth;
                if (isSelected) {
                    fillColor = 'rgba(13, 110, 253, 0.3)';
                    strokeColor = '#0d6efd';
                    lineWidth = 3;
                } else if (isHovered) {
                    fillColor = 'rgba(255, 193, 7, 0.2)';
                    strokeColor = '#ffc107';
                    lineWidth = 2;
                } else {
                    fillColor = 'rgba(108, 117, 125, 0.1)';
                    strokeColor = '#6c757d';
                    lineWidth = 1;
                }

                if (region.rectangles && region.rectangles.length > 0) {
                    region.rectangles.forEach(rect => {
                        // Convert world to screen coordinates
                        const screenX = rect.x * this.scale + this.panX;
                        const screenY = rect.y * this.scale + this.panY;
                        const screenW = rect.width * this.scale;
                        const screenH = rect.height * this.scale;

                        // Fill
                        this.ctx.fillStyle = fillColor;
                        this.ctx.fillRect(screenX, screenY, screenW, screenH);

                        // Stroke
                        this.ctx.strokeStyle = strokeColor;
                        this.ctx.lineWidth = lineWidth;
                        this.ctx.strokeRect(screenX, screenY, screenW, screenH);
                    });

                    // Draw region name label (on first rectangle only)
                    if (isSelected || isHovered) {
                        const firstRect = region.rectangles[0];
                        const centerX = (firstRect.x + firstRect.width / 2) * this.scale + this.panX;
                        const centerY = (firstRect.y + firstRect.height / 2) * this.scale + this.panY;

                        this.ctx.font = 'bold 12px Arial';
                        this.ctx.textAlign = 'center';
                        this.ctx.textBaseline = 'middle';

                        // Draw text background
                        const textMetrics = this.ctx.measureText(region.name);
                        const textWidth = textMetrics.width + 8;
                        const textHeight = 16;
                        this.ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                        this.ctx.fillRect(centerX - textWidth / 2, centerY - textHeight / 2, textWidth, textHeight);

                        // Draw text
                        this.ctx.fillStyle = isSelected ? '#ffffff' : '#ffed4e';
                        this.ctx.fillText(region.name, centerX, centerY);
                    }
                }
            });
        }

        // Draw XML spawners (green, underneath user spawns)
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
    },
    
    // Smooth keyboard panning functions
    addKey: function(key) {
        const wasEmpty = this.keyStates.size === 0;
        this.keyStates.add(key.toLowerCase());
        
        // Start animation loop if this is the first key
        if (wasEmpty) {
            this.startKeyPanning();
        }
    },
    
    removeKey: function(key) {
        this.keyStates.delete(key.toLowerCase());
    },
    
    startKeyPanning: function() {
        if (this.panAnimationId !== null) return; // Already running
        
        const panStep = 5; // pixels per frame at 60fps (~300 pixels/second)
        
        const animate = () => {
            if (this.keyStates.size === 0) {
                this.panAnimationId = null;
                return; // Stop animation when no keys pressed
            }
            
            let deltaX = 0;
            let deltaY = 0;
            
            // Check all pressed keys and accumulate movement (supports diagonals!)
            if (this.keyStates.has('w') || this.keyStates.has('arrowup')) deltaY += panStep;
            if (this.keyStates.has('s') || this.keyStates.has('arrowdown')) deltaY -= panStep;
            if (this.keyStates.has('a') || this.keyStates.has('arrowleft')) deltaX += panStep;
            if (this.keyStates.has('d') || this.keyStates.has('arrowright')) deltaX -= panStep;
            
            // Apply pan
            this.panX += deltaX;
            this.panY += deltaY;
            this.applyPan(this.panX, this.panY);
            
            // Continue animation
            this.panAnimationId = requestAnimationFrame(animate);
        };

        this.panAnimationId = requestAnimationFrame(animate);
    },

    getPanPosition: function() {
        return { x: this.panX, y: this.panY };
    },

    getPan: function() {
        return [this.panX, this.panY];
    },

    getWorldCoordinates: function(screenX, screenY) {
        const worldX = Math.floor((screenX - this.panX) / this.scale);
        const worldY = Math.floor((screenY - this.panY) / this.scale);
        return [worldX, worldY];
    },

    showRegions: function(regions, selectedName, hoveredName) {
        console.log(`🗺️ Showing ${regions ? regions.length : 0} regions`);
        this.regions = regions || [];
        this.selectedRegionName = selectedName || '';
        this.hoveredRegionName = hoveredName || '';
        this.redrawAll();
    },

    hideRegions: function() {
        console.log(`🗺️ Hiding regions`);
        this.regions = null;
        this.selectedRegionName = '';
        this.hoveredRegionName = '';
        this.redrawAll();
    }
};

console.log('??? Map module loaded');





/**
 * Draw regions on the region map canvas
 * @param {string} canvasId - ID of the canvas element
 * @param {Array} regions - Array of region objects with Name, MapId, and Rectangles
 * @param {string} selectedRegionName - Name of the currently selected region
 * @param {string} hoveredRegionName - Name of the currently hovered region
 */
window.drawRegions = function(canvasId, regions, selectedRegionName, hoveredRegionName) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error('Canvas not found:', canvasId);
        return;
    }
    
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    if (!regions || regions.length === 0) {
        return;
    }
    
    // Draw each region
    regions.forEach(region => {
        const isSelected = region.name === selectedRegionName;
        const isHovered = region.name === hoveredRegionName;
        
        // Determine color and opacity
        let fillColor, strokeColor, lineWidth;
        if (isSelected) {
            fillColor = 'rgba(13, 110, 253, 0.3)'; // Blue with 30% opacity
            strokeColor = '#0d6efd';
            lineWidth = 3;
        } else if (isHovered) {
            fillColor = 'rgba(255, 193, 7, 0.2)'; // Yellow with 20% opacity
            strokeColor = '#ffc107';
            lineWidth = 2;
        } else {
            fillColor = 'rgba(108, 117, 125, 0.1)'; // Gray with 10% opacity
            strokeColor = '#6c757d';
            lineWidth = 1;
        }
        
        // Draw all rectangles for this region
        if (region.rectangles && region.rectangles.length > 0) {
            region.rectangles.forEach(rect => {
                // Fill
                ctx.fillStyle = fillColor;
                ctx.fillRect(rect.x, rect.y, rect.width, rect.height);
                
                // Stroke
                ctx.strokeStyle = strokeColor;
                ctx.lineWidth = lineWidth;
                ctx.strokeRect(rect.x, rect.y, rect.width, rect.height);
            });
        }
        
        // Draw region name label (on first rectangle only)
        if (region.rectangles && region.rectangles.length > 0 && (isSelected || isHovered)) {
            const firstRect = region.rectangles[0];
            const centerX = firstRect.x + firstRect.width / 2;
            const centerY = firstRect.y + firstRect.height / 2;
            
            ctx.font = 'bold 12px Arial';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            
            // Draw text background
            const textMetrics = ctx.measureText(region.name);
            const textWidth = textMetrics.width + 8;
            const textHeight = 16;
            ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
            ctx.fillRect(centerX - textWidth / 2, centerY - textHeight / 2, textWidth, textHeight);

            // Draw text
            ctx.fillStyle = isSelected ? '#ffffff' : '#ffed4e';
            ctx.fillText(region.name, centerX, centerY);
        }
    });
};

/**
 * Clears a canvas by canvas ID
 * @param {string} canvasId - ID of the canvas element to clear
 */
window.clearCanvas = function(canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error('Canvas not found:', canvasId);
        return;
    }

    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);
};

console.log('Region drawing function loaded');
