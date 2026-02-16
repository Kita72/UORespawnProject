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
hoveredAreaIndex: -1,  // Index of hovered area within region (-1 = none)

// Hovered spawn for box spawn page
hoveredSpawnId: null,

// XML spawner hover/click state
hoveredXmlSpawnerId: null,
selectedXmlSpawner: null,
xmlTooltipMousePos: { x: 0, y: 0 },

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
        this.hoveredAreaIndex = -1;
        this.hoveredSpawnId = null;

        console.log(`? Map initialized: ${imgWidth}x${imgHeight} at ${this.scale}x scale, viewport: ${this.viewportWidth}x${this.viewportHeight}`);
        return true;
    },

    // Set hovered region area for highlighting
    setHoveredRegionArea: function(regionName, areaIndex) {
        if (this.hoveredRegionName !== regionName || this.hoveredAreaIndex !== areaIndex) {
            this.hoveredRegionName = regionName || '';
            this.hoveredAreaIndex = areaIndex >= 0 ? areaIndex : -1;
            this.redrawAll();
        }
    },

    // Set hovered spawn for highlighting
    setHoveredSpawn: function(spawnId) {
        if (this.hoveredSpawnId !== spawnId) {
            this.hoveredSpawnId = spawnId;
            this.redrawAll();
        }
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

    // Calculate color based on priority using hue rotation for distinct visual levels
    // Supports up to 25 priority levels with clearly distinguishable colors
    getColorForPriority: function(priority) {
        // Priority 0 uses the user's configured base color
        if (priority === 0) {
            return this.boxColor;
        }

        // Define 24 distinct colors for priorities 1-24
        // Colors chosen to be visually distinct from each other AND from default red
        const priorityColors = [
            '#FF8C00', // 1 - Dark Orange (distinct from red base)
            '#FFD700', // 2 - Gold
            '#FFFF00', // 3 - Yellow
            '#ADFF2F', // 4 - Green Yellow
            '#7FFF00', // 5 - Chartreuse
            '#00FF00', // 6 - Lime
            '#00FA9A', // 7 - Medium Spring Green
            '#00FFFF', // 8 - Cyan
            '#00BFFF', // 9 - Deep Sky Blue
            '#1E90FF', // 10 - Dodger Blue
            '#0000FF', // 11 - Blue
            '#8A2BE2', // 12 - Blue Violet
            '#9400D3', // 13 - Dark Violet
            '#FF00FF', // 14 - Magenta
            '#FF1493', // 15 - Deep Pink
            '#FF69B4', // 16 - Hot Pink
            '#DC143C', // 17 - Crimson
            '#FF4500', // 18 - Orange Red
            '#FFA500', // 19 - Orange
            '#98FB98', // 20 - Pale Green
            '#87CEEB', // 21 - Sky Blue
            '#DDA0DD', // 22 - Plum
            '#F0E68C', // 23 - Khaki
            '#FFFFFF'  // 24 - White (maximum priority)
        ];

        // Priority 1 uses index 0, Priority 2 uses index 1, etc.
        const colorIndex = Math.min(priority - 1, priorityColors.length - 1);
        const resultColor = priorityColors[Math.max(0, colorIndex)];

        console.log(`Priority ${priority}: Using color ${resultColor}`);

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
                const isRegionHovered = region.name === this.hoveredRegionName;

                if (region.rectangles && region.rectangles.length > 0) {
                    region.rectangles.forEach((rect, areaIndex) => {
                        // Check if this specific area is hovered
                        const isAreaHovered = isRegionHovered && this.hoveredAreaIndex === areaIndex;

                        let fillColor, strokeColor, lineWidth;
                        if (isSelected) {
                            if (isAreaHovered) {
                                // Selected region, hovered area - extra bright
                                fillColor = 'rgba(13, 110, 253, 0.45)';
                                strokeColor = '#4da3ff';
                                lineWidth = 4;
                            } else {
                                // Selected region, non-hovered area
                                fillColor = 'rgba(13, 110, 253, 0.25)';
                                strokeColor = '#0d6efd';
                                lineWidth = 2;
                            }
                        } else if (isAreaHovered) {
                            // Not selected, but this area is hovered - golden highlight
                            fillColor = 'rgba(255, 215, 0, 0.25)';
                            strokeColor = '#FFD700';
                            lineWidth = 3;
                        } else if (isRegionHovered) {
                            // Region hovered but different area
                            fillColor = 'rgba(255, 193, 7, 0.15)';
                            strokeColor = '#ffc107';
                            lineWidth = 2;
                        } else {
                            // Default unselected, unhovered
                            fillColor = 'rgba(108, 117, 125, 0.1)';
                            strokeColor = '#6c757d';
                            lineWidth = 1;
                        }

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

                        // Draw golden glow for hovered area
                        if (isAreaHovered) {
                            this.ctx.strokeStyle = 'rgba(255, 215, 0, 0.5)';
                            this.ctx.lineWidth = lineWidth + 3;
                            this.ctx.strokeRect(screenX - 2, screenY - 2, screenW + 4, screenH + 4);
                        }
                    });

                    // Draw region name label on hovered area (or first rect if just region selected)
                    if (isSelected || isRegionHovered) {
                        let labelRect;
                        if (isRegionHovered && this.hoveredAreaIndex >= 0 && this.hoveredAreaIndex < region.rectangles.length) {
                            labelRect = region.rectangles[this.hoveredAreaIndex];
                        } else {
                            labelRect = region.rectangles[0];
                        }

                        const centerX = (labelRect.x + labelRect.width / 2) * this.scale + this.panX;
                        const centerY = (labelRect.y + labelRect.height / 2) * this.scale + this.panY;

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
                        this.ctx.fillStyle = isSelected ? '#ffffff' : '#FFD700';
                        this.ctx.fillText(region.name, centerX, centerY);
                    }
                }
            });
        }

        // Draw XML spawners (green circles, underneath user spawns)
        if (this.xmlSpawners && Array.isArray(this.xmlSpawners)) {
            this.xmlSpawners.forEach((spawner, idx) => {
                if (!spawner) return;

                // Use center coordinates and radius for circle visualization
                const centerX = spawner.centerX || spawner.x || 0;
                const centerY = spawner.centerY || spawner.y || 0;
                const radius = spawner.radius || spawner.width || 10;

                // Convert center point to screen coordinates
                const screenCenter = this.worldToScreen(centerX, centerY);

                // Scale the radius to screen size
                const screenRadius = radius * this.scale;

                // Check if this spawner is hovered or selected
                const isHovered = this.hoveredXmlSpawnerId === idx;
                const isSelected = this.selectedXmlSpawner && this.selectedXmlSpawner.idx === idx;

                // Draw XML spawner circle
                if (isHovered || isSelected) {
                    // Draw golden glow for hover/selected
                    this.ctx.strokeStyle = '#FFD700';
                    this.ctx.lineWidth = 3;
                    this.ctx.globalAlpha = 0.8;
                    this.ctx.beginPath();
                    this.ctx.arc(screenCenter.x, screenCenter.y, screenRadius + 2, 0, 2 * Math.PI);
                    this.ctx.stroke();

                    // Fill with semi-transparent gold
                    this.ctx.fillStyle = 'rgba(255, 215, 0, 0.15)';
                    this.ctx.globalAlpha = 1.0;
                    this.ctx.beginPath();
                    this.ctx.arc(screenCenter.x, screenCenter.y, screenRadius, 0, 2 * Math.PI);
                    this.ctx.fill();
                }

                // Normal circle outline
                this.ctx.strokeStyle = isHovered || isSelected ? '#00FF00' : '#00FF00';
                this.ctx.lineWidth = isHovered || isSelected ? 2 : 1;
                this.ctx.globalAlpha = isHovered || isSelected ? 0.8 : 0.4;
                this.ctx.beginPath();
                this.ctx.arc(screenCenter.x, screenCenter.y, screenRadius, 0, 2 * Math.PI);
                this.ctx.stroke();

                // Draw "X" marker in center
                const markerSize = isHovered || isSelected ? 5 : 3;
                this.ctx.strokeStyle = '#00FF00';
                this.ctx.lineWidth = isHovered || isSelected ? 3 : 2;
                this.ctx.globalAlpha = isHovered || isSelected ? 0.9 : 0.6;
                this.ctx.beginPath();
                this.ctx.moveTo(screenCenter.x - markerSize, screenCenter.y - markerSize);
                this.ctx.lineTo(screenCenter.x + markerSize, screenCenter.y + markerSize);
                this.ctx.moveTo(screenCenter.x + markerSize, screenCenter.y - markerSize);
                this.ctx.lineTo(screenCenter.x - markerSize, screenCenter.y + markerSize);
                this.ctx.stroke();
                this.ctx.globalAlpha = 1.0;
            });

            // Draw info tooltip for selected XML spawner (at mouse position)
            if (this.selectedXmlSpawner) {
                const spawner = this.selectedXmlSpawner;
                const mouseX = this.xmlTooltipMousePos.x;
                const mouseY = this.xmlTooltipMousePos.y;

                // Tooltip content
                const lines = [
                    `XML Spawner`,
                    `Location: (${spawner.centerX}, ${spawner.centerY})`,
                    `Home Range: ${spawner.radius}`
                ];

                // Measure text for sizing
                this.ctx.font = 'bold 12px Arial';
                let maxWidth = 0;
                lines.forEach(line => {
                    const width = this.ctx.measureText(line).width;
                    if (width > maxWidth) maxWidth = width;
                });

                const padding = 8;
                const lineHeight = 16;
                const boxWidth = maxWidth + padding * 2;
                const boxHeight = lines.length * lineHeight + padding * 2;

                // Position tooltip near mouse (offset to not cover cursor)
                let tooltipX = mouseX + 15;
                let tooltipY = mouseY + 15;

                // Keep tooltip within canvas bounds
                if (tooltipX + boxWidth > this.viewportWidth) {
                    tooltipX = mouseX - boxWidth - 10;
                }
                if (tooltipY + boxHeight > this.viewportHeight) {
                    tooltipY = mouseY - boxHeight - 10;
                }

                // Draw tooltip background
                this.ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
                this.ctx.strokeStyle = '#00FF00';
                this.ctx.lineWidth = 2;
                this.ctx.beginPath();
                this.ctx.roundRect(tooltipX, tooltipY, boxWidth, boxHeight, 5);
                this.ctx.fill();
                this.ctx.stroke();

                // Draw tooltip text
                this.ctx.fillStyle = '#00FF00';
                this.ctx.font = 'bold 12px Arial';
                this.ctx.textAlign = 'left';
                this.ctx.textBaseline = 'top';
                lines.forEach((line, i) => {
                    if (i === 0) {
                        this.ctx.fillStyle = '#FFD700'; // Title in gold
                    } else {
                        this.ctx.fillStyle = '#FFFFFF'; // Content in white
                    }
                    this.ctx.fillText(line, tooltipX + padding, tooltipY + padding + i * lineHeight);
                });
            }
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

                // Get priority (default 0 if not set) - uses camelCase from C# serialization
                const priority = spawn.priority || 0;
                const pos = spawn.position || (idx + 1);
                const isHovered = this.hoveredSpawnId === pos;

                // Draw box with priority-based color
                this.ctx.strokeStyle = this.getColorForPriority(priority);
                this.ctx.lineWidth = this.boxLineSize;
                this.ctx.strokeRect(topLeft.x, topLeft.y, screenW, screenH);

                // Draw hover highlight effect (golden glow)
                if (isHovered) {
                    // Draw outer glow
                    this.ctx.strokeStyle = '#FFD700';
                    this.ctx.lineWidth = this.boxLineSize + 4;
                    this.ctx.globalAlpha = 0.5;
                    this.ctx.strokeRect(topLeft.x - 2, topLeft.y - 2, screenW + 4, screenH + 4);

                    // Draw inner bright border
                    this.ctx.strokeStyle = '#FFFFFF';
                    this.ctx.lineWidth = this.boxLineSize + 1;
                    this.ctx.globalAlpha = 0.8;
                    this.ctx.strokeRect(topLeft.x, topLeft.y, screenW, screenH);

                    // Fill with semi-transparent highlight
                    this.ctx.fillStyle = 'rgba(255, 215, 0, 0.15)';
                    this.ctx.globalAlpha = 1.0;
                    this.ctx.fillRect(topLeft.x, topLeft.y, screenW, screenH);

                    this.ctx.globalAlpha = 1.0;
                }

                // Draw position number
                this.ctx.fillStyle = isHovered ? '#FFD700' : '#FFFFFF';
                this.ctx.font = isHovered ? 'bold 18px Arial' : 'bold 16px Arial';
                this.ctx.textAlign = 'center';
                this.ctx.textBaseline = 'top';
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
        this.hoveredXmlSpawnerId = null;
        this.selectedXmlSpawner = null;
        this.redrawAll();
    },

    hideXMLSpawners: function() {
        console.log(`??????? Hiding XML spawners`);
        this.xmlSpawners = null;
        this.hoveredXmlSpawnerId = null;
        this.selectedXmlSpawner = null;
        this.redrawAll();
    },

    // Set hovered XML spawner (called from C#)
    setHoveredXmlSpawner: function(idx) {
        if (this.hoveredXmlSpawnerId !== idx) {
            this.hoveredXmlSpawnerId = idx;
            this.redrawAll();
        }
    },

    // Handle XML spawner click - toggle selection and store mouse position
    handleXmlSpawnerClick: function(idx, mouseX, mouseY) {
        if (!this.xmlSpawners || idx < 0 || idx >= this.xmlSpawners.length) {
            this.selectedXmlSpawner = null;
            this.redrawAll();
            return false;
        }

        // If clicking same spawner, deselect
        if (this.selectedXmlSpawner && this.selectedXmlSpawner.idx === idx) {
            this.selectedXmlSpawner = null;
            this.redrawAll();
            return false;
        }

        // Select new spawner
        const spawner = this.xmlSpawners[idx];
        this.selectedXmlSpawner = {
            idx: idx,
            centerX: spawner.centerX || spawner.x || 0,
            centerY: spawner.centerY || spawner.y || 0,
            radius: spawner.radius || spawner.width || 10
        };
        this.xmlTooltipMousePos = { x: mouseX, y: mouseY };
        this.redrawAll();
        return true;
    },

    // Update tooltip position while hovering with selection active
    updateXmlTooltipPosition: function(mouseX, mouseY) {
        if (this.selectedXmlSpawner) {
            this.xmlTooltipMousePos = { x: mouseX, y: mouseY };
            this.redrawAll();
        }
    },

    // Clear XML spawner selection (called when mouse leaves area)
    clearXmlSpawnerSelection: function() {
        if (this.selectedXmlSpawner) {
            this.selectedXmlSpawner = null;
            this.redrawAll();
        }
    },

    // Find XML spawner at world coordinates (returns index or -1)
    findXmlSpawnerAt: function(worldX, worldY) {
        if (!this.xmlSpawners || !Array.isArray(this.xmlSpawners)) {
            return -1;
        }

        for (let i = 0; i < this.xmlSpawners.length; i++) {
            const spawner = this.xmlSpawners[i];
            if (!spawner) continue;

            const centerX = spawner.centerX || spawner.x || 0;
            const centerY = spawner.centerY || spawner.y || 0;
            const radius = spawner.radius || spawner.width || 10;

            // Check if point is within circle
            const dx = worldX - centerX;
            const dy = worldY - centerY;
            const distSquared = dx * dx + dy * dy;

            if (distSquared <= radius * radius) {
                return i;
            }
        }

        return -1;
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
