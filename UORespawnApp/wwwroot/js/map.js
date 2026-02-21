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

// XML spawner hover state (hover-based like spawn data)
hoveredXmlSpawnerId: null,
xmlSpawnerTooltipVisible: false,
xmlTooltipMousePos: { x: 0, y: 0 },
xmlSpawnerDwellTimer: null,
xmlSpawnerDwellDelay: 500, // milliseconds to wait before showing tooltip

// Server spawn (stats) hover state
hoveredServerSpawnIdx: -1,
serverSpawnTooltipVisible: false,
serverSpawnTooltipMousePos: { x: 0, y: 0 },
serverSpawnDwellTimer: null,
serverSpawnDwellDelay: 500, // milliseconds to wait before showing tooltip
serverSpawnFlashAnimationId: null, // Animation frame ID for spawn dot flashing

// Vendor marker state
vendorMarkers: null,
selectedVendorSignType: '',
hoveredVendorMarkerIdx: -1,
vendorMarkerTooltipVisible: false,
vendorMarkerTooltipMousePos: { x: 0, y: 0 },
vendorMarkerDwellTimer: null,
vendorMarkerDwellDelay: 500, // milliseconds to wait before showing tooltip

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
        this.hoveredServerSpawnIdx = -1;
        this.serverSpawnTooltipVisible = false;
        if (this.serverSpawnDwellTimer) {
            clearTimeout(this.serverSpawnDwellTimer);
            this.serverSpawnDwellTimer = null;
        }
        // Reset XML spawner state
        this.hoveredXmlSpawnerId = null;
        this.xmlSpawnerTooltipVisible = false;
        if (this.xmlSpawnerDwellTimer) {
            clearTimeout(this.xmlSpawnerDwellTimer);
            this.xmlSpawnerDwellTimer = null;
        }
        // Reset vendor marker state
        this.vendorMarkers = null;
        this.selectedVendorSignType = '';
        this.hoveredVendorMarkerIdx = -1;
        this.vendorMarkerTooltipVisible = false;
        if (this.vendorMarkerDwellTimer) {
            clearTimeout(this.vendorMarkerDwellTimer);
            this.vendorMarkerDwellTimer = null;
        }

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
        // Sort by radius descending so smaller circles are drawn on top (more accessible)
        if (this.xmlSpawners && Array.isArray(this.xmlSpawners)) {
            // Create indexed array and sort by radius (largest first = drawn first = bottom layer)
            const sortedSpawners = this.xmlSpawners
                .map((spawner, idx) => ({ spawner, idx }))
                .filter(item => item.spawner != null)
                .sort((a, b) => {
                    const radiusA = a.spawner.radius || a.spawner.width || 10;
                    const radiusB = b.spawner.radius || b.spawner.width || 10;
                    return radiusB - radiusA; // Descending order (largest first)
                });

            sortedSpawners.forEach(({ spawner, idx }) => {

                // Use center coordinates and radius for circle visualization
                const centerX = spawner.centerX || spawner.x || 0;
                const centerY = spawner.centerY || spawner.y || 0;
                const radius = spawner.radius || spawner.width || 10;

                // Convert center point to screen coordinates
                const screenCenter = this.worldToScreen(centerX, centerY);

                // Scale the radius to screen size
                const screenRadius = radius * this.scale;

                // Check if this spawner is hovered (hover-based like spawn data)
                const isHovered = this.hoveredXmlSpawnerId === idx;

                // Draw XML spawner circle
                if (isHovered) {
                    // Draw golden glow for hover
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
                this.ctx.strokeStyle = '#00FF00';
                this.ctx.lineWidth = isHovered ? 2 : 1;
                this.ctx.globalAlpha = isHovered ? 0.8 : 0.4;
                this.ctx.beginPath();
                this.ctx.arc(screenCenter.x, screenCenter.y, screenRadius, 0, 2 * Math.PI);
                this.ctx.stroke();

                // Draw "X" marker in center
                const markerSize = isHovered ? 5 : 3;
                this.ctx.strokeStyle = '#00FF00';
                this.ctx.lineWidth = isHovered ? 3 : 2;
                this.ctx.globalAlpha = isHovered ? 0.9 : 0.6;
                this.ctx.beginPath();
                this.ctx.moveTo(screenCenter.x - markerSize, screenCenter.y - markerSize);
                this.ctx.lineTo(screenCenter.x + markerSize, screenCenter.y + markerSize);
                this.ctx.moveTo(screenCenter.x + markerSize, screenCenter.y - markerSize);
                this.ctx.lineTo(screenCenter.x - markerSize, screenCenter.y + markerSize);
                this.ctx.stroke();
                this.ctx.globalAlpha = 1.0;
            });

            // Draw info tooltip for hovered XML spawner (at mouse position, after dwell)
            if (this.xmlSpawnerTooltipVisible && this.hoveredXmlSpawnerId >= 0 && this.hoveredXmlSpawnerId < this.xmlSpawners.length) {
                const spawner = this.xmlSpawners[this.hoveredXmlSpawnerId];
                const mouseX = this.xmlTooltipMousePos.x;
                const mouseY = this.xmlTooltipMousePos.y;

                // Tooltip content
                const lines = [
                    `XML Spawner`,
                    `Location: (${spawner.centerX}, ${spawner.centerY})`,
                    `Home Range: ${spawner.radius}`
                ];

                // Add MaxCount if available
                if (spawner.maxCount && spawner.maxCount > 0) {
                    lines.push(`Max Count: ${spawner.maxCount}`);
                }

                // Add spawn names if available
                if (spawner.spawnNames && spawner.spawnNames.length > 0) {
                    lines.push(`Creatures:`);
                    // Show up to 8 creatures, then "and X more..."
                    const maxToShow = 8;
                    const displayNames = spawner.spawnNames.slice(0, maxToShow);
                    displayNames.forEach(name => {
                        lines.push(`  • ${name}`);
                    });
                    if (spawner.spawnNames.length > maxToShow) {
                        lines.push(`  ...and ${spawner.spawnNames.length - maxToShow} more`);
                    }
                }

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
                    } else if (line === 'Creatures:') {
                        this.ctx.fillStyle = '#00BFFF'; // Creatures header in blue
                    } else if (line.startsWith('  •') || line.startsWith('  ...')) {
                        this.ctx.fillStyle = '#98FB98'; // Creature names in pale green
                    } else {
                        this.ctx.fillStyle = '#FFFFFF'; // Content in white
                    }
                    this.ctx.fillText(line, tooltipX + padding, tooltipY + padding + i * lineHeight);
                });
            }
        }

        // Draw server spawns (heatmap) - colored dots showing where creatures spawned
        if (this.serverSpawns && Array.isArray(this.serverSpawns)) {
            // Calculate flash pulse for hovered spawn dots (pulsing animation)
            const flashPulse = this.hoveredServerSpawnIdx >= 0 ? 
                0.5 + 0.5 * Math.sin(Date.now() / 150) : 1.0; // Faster pulsing

            this.serverSpawns.forEach((spawn, idx) => {
                if (!spawn) return;

                const isHovered = (this.hoveredServerSpawnIdx === idx);
                const playerColor = `rgb(${spawn.colorR}, ${spawn.colorG}, ${spawn.colorB})`;

                // Draw player location (larger square, 10x10 pixels normally, 14x14 when hovered)
                const playerPos = this.worldToScreen(spawn.playerX, spawn.playerY);
                const playerSize = isHovered ? 7 : 5;

                this.ctx.fillStyle = playerColor;
                this.ctx.globalAlpha = isHovered ? 1.0 : 0.7;
                this.ctx.fillRect(playerPos.x - playerSize, playerPos.y - playerSize, playerSize * 2, playerSize * 2);

                // Draw highlight border when hovered
                if (isHovered) {
                    this.ctx.strokeStyle = '#FFFFFF';
                    this.ctx.lineWidth = 2;
                    this.ctx.strokeRect(playerPos.x - playerSize - 1, playerPos.y - playerSize - 1, playerSize * 2 + 2, playerSize * 2 + 2);
                }

                // Draw spawn location (creature spawn position)
                const spawnPos = this.worldToScreen(spawn.spawnX, spawn.spawnY);

                if (isHovered) {
                    // Flashing/pulsing spawn dot when player is hovered
                    const pulseSize = 4 + 3 * flashPulse; // Pulses between 4 and 7 pixels

                    // Draw outer glow effect
                    this.ctx.fillStyle = '#FFFFFF';
                    this.ctx.globalAlpha = 0.3 * flashPulse;
                    this.ctx.beginPath();
                    this.ctx.arc(spawnPos.x, spawnPos.y, pulseSize + 5, 0, 2 * Math.PI);
                    this.ctx.fill();

                    // Draw middle glow
                    this.ctx.fillStyle = '#FFFFFF';
                    this.ctx.globalAlpha = 0.5 * flashPulse;
                    this.ctx.beginPath();
                    this.ctx.arc(spawnPos.x, spawnPos.y, pulseSize + 2, 0, 2 * Math.PI);
                    this.ctx.fill();

                    // Draw bright spawn dot
                    this.ctx.fillStyle = '#FFFFFF';
                    this.ctx.globalAlpha = 0.7 + 0.3 * flashPulse;
                    this.ctx.beginPath();
                    this.ctx.arc(spawnPos.x, spawnPos.y, pulseSize, 0, 2 * Math.PI);
                    this.ctx.fill();

                    // Draw colored center
                    this.ctx.fillStyle = playerColor;
                    this.ctx.globalAlpha = 1.0;
                    this.ctx.beginPath();
                    this.ctx.arc(spawnPos.x, spawnPos.y, pulseSize - 2, 0, 2 * Math.PI);
                    this.ctx.fill();

                    // Draw connecting line from player to spawn location
                    this.ctx.strokeStyle = playerColor;
                    this.ctx.lineWidth = 2;
                    this.ctx.globalAlpha = 0.6;
                    this.ctx.setLineDash([4, 4]); // Dashed line
                    this.ctx.beginPath();
                    this.ctx.moveTo(playerPos.x, playerPos.y);
                    this.ctx.lineTo(spawnPos.x, spawnPos.y);
                    this.ctx.stroke();
                    this.ctx.setLineDash([]); // Reset to solid
                } else {
                    // Normal spawn dot - small circle with contrasting border
                    const spawnSize = 3;

                    // Draw white border for visibility
                    this.ctx.fillStyle = '#FFFFFF';
                    this.ctx.globalAlpha = 0.5;
                    this.ctx.beginPath();
                    this.ctx.arc(spawnPos.x, spawnPos.y, spawnSize + 1, 0, 2 * Math.PI);
                    this.ctx.fill();

                    // Draw colored center
                    this.ctx.fillStyle = playerColor;
                    this.ctx.globalAlpha = 0.9;
                    this.ctx.beginPath();
                    this.ctx.arc(spawnPos.x, spawnPos.y, spawnSize, 0, 2 * Math.PI);
                    this.ctx.fill();
                }

                this.ctx.globalAlpha = 1.0;
            });

            // Draw tooltip for hovered server spawn (after all dots so it's on top)
            if (this.serverSpawnTooltipVisible && this.hoveredServerSpawnIdx >= 0 && this.hoveredServerSpawnIdx < this.serverSpawns.length) {
                const spawn = this.serverSpawns[this.hoveredServerSpawnIdx];
                const mouseX = this.serverSpawnTooltipMousePos.x;
                const mouseY = this.serverSpawnTooltipMousePos.y;

                // Tooltip content
                const lines = [
                    `${spawn.playerName}`,
                    `Location: (${spawn.playerX}, ${spawn.playerY})`,
                    `Total Events: ${spawn.totalDotsForPlayer}`
                ];

                // Add creature name if available
                if (spawn.creatureName && spawn.creatureName.length > 0) {
                    lines.push(`Spawned: ${spawn.creatureName}`);
                }

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
                const playerColor = `rgb(${spawn.colorR}, ${spawn.colorG}, ${spawn.colorB})`;
                this.ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
                this.ctx.strokeStyle = playerColor;
                this.ctx.lineWidth = 2;
                this.ctx.beginPath();
                this.ctx.roundRect(tooltipX, tooltipY, boxWidth, boxHeight, 5);
                this.ctx.fill();
                this.ctx.stroke();

                // Draw tooltip text
                this.ctx.font = 'bold 12px Arial';
                this.ctx.textAlign = 'left';
                this.ctx.textBaseline = 'top';
                lines.forEach((line, i) => {
                    if (i === 0) {
                        this.ctx.fillStyle = playerColor; // Player name in their color
                    } else if (line.startsWith('Spawned:')) {
                        this.ctx.fillStyle = '#98FB98'; // Creature name in pale green
                    } else {
                        this.ctx.fillStyle = '#FFFFFF'; // Content in white
                    }
                    this.ctx.fillText(line, tooltipX + padding, tooltipY + padding + i * lineHeight);
                });
            }
        }

        // Draw vendor markers (sign/hive locations)
        // Sort markers so focused/selected render last (on top)
        if (this.vendorMarkers && Array.isArray(this.vendorMarkers)) {
            // Create indexed array for sorting while preserving original indices
            const sortedMarkers = this.vendorMarkers.map((marker, idx) => ({ marker, idx }))
                .filter(item => item.marker)
                .sort((a, b) => {
                    const aFocused = a.marker.isFocused === true;
                    const bFocused = b.marker.isFocused === true;
                    const aSelected = a.marker.signType === this.selectedVendorSignType;
                    const bSelected = b.marker.signType === this.selectedVendorSignType;
                    const aHovered = this.hoveredVendorMarkerIdx === a.idx;
                    const bHovered = this.hoveredVendorMarkerIdx === b.idx;

                    // Priority: focused > hovered > selected > normal
                    const aPriority = aFocused ? 3 : (aHovered ? 2 : (aSelected ? 1 : 0));
                    const bPriority = bFocused ? 3 : (bHovered ? 2 : (bSelected ? 1 : 0));
                    return aPriority - bPriority;
                });

            sortedMarkers.forEach(({ marker, idx }) => {
                const screenPos = this.worldToScreen(marker.x, marker.y);
                const isHovered = this.hoveredVendorMarkerIdx === idx;
                const isSelected = marker.signType === this.selectedVendorSignType;
                const isFocused = marker.isFocused === true;
                const isHive = marker.type === 'hive';
                const hasVendors = marker.hasVendors;
                const signType = marker.signType || '';

                // Get icon category and color for this sign type
                const iconInfo = this.getVendorIconInfo(signType, isHive, hasVendors, isFocused);

                // Base size - focused marker is larger
                const baseSize = isHive ? 7 : 6;
                const size = isFocused ? baseSize + 4 : (isHovered ? baseSize + 3 : (isSelected ? baseSize + 2 : baseSize));

                // Draw pulsing outer ring for focused marker
                if (isFocused) {
                    this.ctx.strokeStyle = '#FF4500';
                    this.ctx.lineWidth = 3;
                    this.ctx.globalAlpha = 0.8;
                    this.ctx.beginPath();
                    this.ctx.arc(screenPos.x, screenPos.y, size + 8, 0, 2 * Math.PI);
                    this.ctx.stroke();

                    // Inner glow
                    this.ctx.fillStyle = '#FF4500';
                    this.ctx.globalAlpha = 0.25;
                    this.ctx.beginPath();
                    this.ctx.arc(screenPos.x, screenPos.y, size + 8, 0, 2 * Math.PI);
                    this.ctx.fill();
                }
                // Draw outer glow for selected/hovered
                else if (isSelected || isHovered) {
                    this.ctx.fillStyle = isHovered ? '#FFD700' : iconInfo.color;
                    this.ctx.globalAlpha = 0.3;
                    this.ctx.beginPath();
                    this.ctx.arc(screenPos.x, screenPos.y, size + 6, 0, 2 * Math.PI);
                    this.ctx.fill();
                }

                // Draw white border (thicker for focused)
                this.ctx.fillStyle = '#FFFFFF';
                this.ctx.globalAlpha = isFocused ? 1.0 : (isHovered ? 1.0 : 0.8);
                this.ctx.beginPath();
                this.ctx.arc(screenPos.x, screenPos.y, size + (isFocused ? 3 : 2), 0, 2 * Math.PI);
                this.ctx.fill();

                // Draw colored center
                this.ctx.fillStyle = isFocused ? '#FF4500' : iconInfo.color;
                this.ctx.globalAlpha = 1.0;
                this.ctx.beginPath();
                this.ctx.arc(screenPos.x, screenPos.y, size, 0, 2 * Math.PI);
                this.ctx.fill();

                // Draw the appropriate icon
                this.drawVendorIcon(screenPos.x, screenPos.y, size, iconInfo.icon, isFocused);

                // Draw vendor count badge if has vendors
                if (hasVendors && marker.vendorCount > 0) {
                    const badgeX = screenPos.x + size;
                    const badgeY = screenPos.y - size;
                    const badgeRadius = 6;

                    // Badge background
                    this.ctx.fillStyle = '#DC3545';
                    this.ctx.beginPath();
                    this.ctx.arc(badgeX, badgeY, badgeRadius, 0, 2 * Math.PI);
                    this.ctx.fill();

                    // Badge text
                    this.ctx.fillStyle = '#FFFFFF';
                    this.ctx.font = 'bold 8px Arial';
                    this.ctx.textAlign = 'center';
                    this.ctx.textBaseline = 'middle';
                    this.ctx.fillText(marker.vendorCount.toString(), badgeX, badgeY);
                }

                this.ctx.globalAlpha = 1.0;
            });

            // Draw tooltip for hovered vendor marker
            if (this.vendorMarkerTooltipVisible && this.hoveredVendorMarkerIdx >= 0 && this.hoveredVendorMarkerIdx < this.vendorMarkers.length) {
                const marker = this.vendorMarkers[this.hoveredVendorMarkerIdx];
                const mouseX = this.vendorMarkerTooltipMousePos.x;
                const mouseY = this.vendorMarkerTooltipMousePos.y;

                // Tooltip content
                const isHive = marker.type === 'hive';
                const typeName = isHive ? 'Hive (Beekeeper)' : marker.signType;
                const lines = [
                    typeName,
                    `Location: (${marker.x}, ${marker.y})`,
                    `Facing: ${marker.facing}`
                ];

                if (marker.hasVendors) {
                    lines.push(`Vendors: ${marker.vendorCount}`);
                } else {
                    lines.push(`No vendors assigned`);
                }

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

                // Position tooltip near mouse
                let tooltipX = mouseX + 15;
                let tooltipY = mouseY + 15;

                if (tooltipX + boxWidth > this.viewportWidth) {
                    tooltipX = mouseX - boxWidth - 10;
                }
                if (tooltipY + boxHeight > this.viewportHeight) {
                    tooltipY = mouseY - boxHeight - 10;
                }

                // Draw tooltip background
                const borderColor = isHive ? '#FFC107' : '#17A2B8';
                this.ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
                this.ctx.strokeStyle = borderColor;
                this.ctx.lineWidth = 2;
                this.ctx.beginPath();
                this.ctx.roundRect(tooltipX, tooltipY, boxWidth, boxHeight, 5);
                this.ctx.fill();
                this.ctx.stroke();

                // Draw tooltip text
                this.ctx.font = 'bold 12px Arial';
                this.ctx.textAlign = 'left';
                this.ctx.textBaseline = 'top';
                lines.forEach((line, i) => {
                    if (i === 0) {
                        this.ctx.fillStyle = borderColor; // Type name in marker color
                    } else if (line.includes('Vendors:')) {
                        this.ctx.fillStyle = '#28A745'; // Vendor count in green
                    } else if (line === 'No vendors assigned') {
                        this.ctx.fillStyle = '#6C757D'; // Gray for no vendors
                    } else {
                        this.ctx.fillStyle = '#FFFFFF'; // Content in white
                    }
                    this.ctx.fillText(line, tooltipX + padding, tooltipY + padding + i * lineHeight);
                });
            }
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
        this.xmlSpawnerTooltipVisible = false;
        if (this.xmlSpawnerDwellTimer) {
            clearTimeout(this.xmlSpawnerDwellTimer);
            this.xmlSpawnerDwellTimer = null;
        }
        this.redrawAll();
    },

    hideXMLSpawners: function() {
        console.log(`??????? Hiding XML spawners`);
        this.xmlSpawners = null;
        this.hoveredXmlSpawnerId = null;
        this.xmlSpawnerTooltipVisible = false;
        if (this.xmlSpawnerDwellTimer) {
            clearTimeout(this.xmlSpawnerDwellTimer);
            this.xmlSpawnerDwellTimer = null;
        }
        this.redrawAll();
    },

    // Called when mouse moves - starts dwell timer for XML spawner tooltip (like spawn data)
    updateXmlSpawnerHover: function(screenX, screenY) {
        const worldPos = this.screenToWorld(screenX, screenY);
        const newIdx = this.findXmlSpawnerAt(worldPos.x, worldPos.y);

        // Clear existing dwell timer
        if (this.xmlSpawnerDwellTimer) {
            clearTimeout(this.xmlSpawnerDwellTimer);
            this.xmlSpawnerDwellTimer = null;
        }

        // If mouse moved to a different spawner or off spawners
        if (newIdx !== this.hoveredXmlSpawnerId) {
            this.hoveredXmlSpawnerId = newIdx;
            this.xmlSpawnerTooltipVisible = false; // Hide tooltip immediately on move
            this.redrawAll();
        }

        // If hovering over a spawner, start dwell timer to show tooltip
        if (newIdx >= 0) {
            this.xmlTooltipMousePos = { x: screenX, y: screenY };
            this.xmlSpawnerDwellTimer = setTimeout(() => {
                if (this.hoveredXmlSpawnerId === newIdx) {
                    this.xmlSpawnerTooltipVisible = true;
                    this.redrawAll();
                }
            }, this.xmlSpawnerDwellDelay);
        }
    },

    // Clear XML spawner hover state (called when mouse leaves canvas)
    clearXmlSpawnerHover: function() {
        if (this.xmlSpawnerDwellTimer) {
            clearTimeout(this.xmlSpawnerDwellTimer);
            this.xmlSpawnerDwellTimer = null;
        }
        if (this.hoveredXmlSpawnerId >= 0 || this.xmlSpawnerTooltipVisible) {
            this.hoveredXmlSpawnerId = -1;
            this.xmlSpawnerTooltipVisible = false;
            this.redrawAll();
        }
    },

    // Find XML spawner at world coordinates (returns index or -1)
    // Checks in reverse radius order so smaller circles (on top) are found first
    findXmlSpawnerAt: function(worldX, worldY) {
        if (!this.xmlSpawners || !Array.isArray(this.xmlSpawners)) {
            return -1;
        }

        // Create list of spawners that contain the point, with their radii
        const containingSpawners = [];

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
                containingSpawners.push({ idx: i, radius: radius });
            }
        }

        // Return the one with smallest radius (it's on top visually)
        if (containingSpawners.length === 0) {
            return -1;
        }

        containingSpawners.sort((a, b) => a.radius - b.radius);
        return containingSpawners[0].idx;
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
        this.hoveredServerSpawnIdx = -1;
        this.serverSpawnTooltipVisible = false;
        if (this.serverSpawnDwellTimer) {
            clearTimeout(this.serverSpawnDwellTimer);
            this.serverSpawnDwellTimer = null;
        }
        this.stopSpawnFlashAnimation();
        this.redrawAll();
    },

    // Find server spawn at screen coordinates (returns index or -1)
    // Checks player location squares (10x10 pixels centered on position)
    findServerSpawnAtScreen: function(screenX, screenY) {
        if (!this.serverSpawns || !Array.isArray(this.serverSpawns)) {
            return -1;
        }

        // Check in reverse order so most recently drawn (on top) is found first
        for (let i = this.serverSpawns.length - 1; i >= 0; i--) {
            const spawn = this.serverSpawns[i];
            if (!spawn) continue;

            // Check player location square (10x10 pixels, centered)
            const playerPos = this.worldToScreen(spawn.playerX, spawn.playerY);
            const halfSize = 5;

            if (screenX >= playerPos.x - halfSize && screenX <= playerPos.x + halfSize &&
                screenY >= playerPos.y - halfSize && screenY <= playerPos.y + halfSize) {
                return i;
            }
        }

        return -1;
    },

    // Called when mouse moves - starts dwell timer for tooltip
    updateServerSpawnHover: function(screenX, screenY) {
        const newIdx = this.findServerSpawnAtScreen(screenX, screenY);

        // Clear existing dwell timer
        if (this.serverSpawnDwellTimer) {
            clearTimeout(this.serverSpawnDwellTimer);
            this.serverSpawnDwellTimer = null;
        }

        // If mouse moved to a different spawn or off spawns
        if (newIdx !== this.hoveredServerSpawnIdx) {
            this.hoveredServerSpawnIdx = newIdx;
            this.serverSpawnTooltipVisible = false; // Hide tooltip immediately on move
            this.redrawAll();
        }

        // If hovering over a spawn, start dwell timer to show tooltip and start flash animation
        if (newIdx >= 0) {
            this.serverSpawnTooltipMousePos = { x: screenX, y: screenY };
            this.serverSpawnDwellTimer = setTimeout(() => {
                if (this.hoveredServerSpawnIdx === newIdx) {
                    this.serverSpawnTooltipVisible = true;
                    this.redrawAll();
                }
            }, this.serverSpawnDwellDelay);

            // Start flash animation for spawn dot
            this.startSpawnFlashAnimation();
        } else {
            // Stop flash animation when not hovering
            this.stopSpawnFlashAnimation();
        }
    },

    // Start the spawn dot flash animation loop
    startSpawnFlashAnimation: function() {
        if (this.serverSpawnFlashAnimationId !== null) return; // Already running

        const animate = () => {
            if (this.hoveredServerSpawnIdx < 0) {
                this.serverSpawnFlashAnimationId = null;
                return; // Stop animation when not hovering
            }

            this.redrawAll();
            this.serverSpawnFlashAnimationId = requestAnimationFrame(animate);
        };

        this.serverSpawnFlashAnimationId = requestAnimationFrame(animate);
    },

    // Stop the spawn dot flash animation
    stopSpawnFlashAnimation: function() {
        if (this.serverSpawnFlashAnimationId !== null) {
            cancelAnimationFrame(this.serverSpawnFlashAnimationId);
            this.serverSpawnFlashAnimationId = null;
        }
    },

    // Clear server spawn hover state (called when mouse leaves canvas)
    clearServerSpawnHover: function() {
        if (this.serverSpawnDwellTimer) {
            clearTimeout(this.serverSpawnDwellTimer);
            this.serverSpawnDwellTimer = null;
        }
        this.stopSpawnFlashAnimation();
        if (this.hoveredServerSpawnIdx >= 0 || this.serverSpawnTooltipVisible) {
            this.hoveredServerSpawnIdx = -1;
            this.serverSpawnTooltipVisible = false;
            this.redrawAll();
        }
    },

    // Vendor icon info helper - returns color and icon type based on sign type
    getVendorIconInfo: function(signType, isHive, hasVendors, isFocused) {
        // Default values
        let color = '#17A2B8'; // Cyan
        let icon = 'sign';

        if (isHive) {
            color = '#FFD700'; // Gold yellow for hives
            icon = 'beehive';
        } else {
            // Categorize sign types
            const guilds = ['ArmamentsGuild', 'ArmourersGuild', 'BlacksmithsGuild', 'WeaponsGuild', 
                           'BardicGuild', 'BartersGuild', 'ProvisionersGuild', 'TradersGuild',
                           'CooksGuild', 'HealersGuild', 'MagesGuild', 'SorcerersGuild', 
                           'IllusionistGuild', 'MinersGuild', 'ArchersGuild', 'SeamensGuild',
                           'FishermensGuild', 'SailorsGuild', 'ShipwrightsGuild', 'TailorsGuild',
                           'ThievesGuild', 'RoguesGuild', 'AssassinsGuild', 'TinkersGuild',
                           'WarriorsGuild', 'CavalryGuild', 'FightersGuild', 'MerchantsGuild'];

            const foodShops = ['Bakery', 'Butcher', 'Tavern', 'Inn'];
            const magicShops = ['Mage', 'ReagentShop'];
            const weaponShops = ['Blacksmith', 'Armourer', 'Fletcher', 'Bowyer'];
            const serviceShops = ['Bank', 'Healer', 'Stables'];
            const craftShops = ['Tailor', 'Tinker', 'Woodworker', 'Jeweler', 'Painter'];
            const woodenSigns = ['WoodenSign'];
            const brassSigns = ['BrassSign'];

            if (guilds.includes(signType)) {
                color = '#9B59B6'; // Purple for guilds
                icon = 'guild';
            } else if (woodenSigns.includes(signType)) {
                color = '#8B4513'; // Saddle brown for wooden
                icon = 'wooden';
            } else if (brassSigns.includes(signType)) {
                color = '#DAA520'; // Goldenrod for brass
                icon = 'brass';
            } else if (foodShops.includes(signType)) {
                color = '#E67E22'; // Orange for food
                icon = 'food';
            } else if (magicShops.includes(signType)) {
                color = '#8E44AD'; // Deep purple for magic
                icon = 'magic';
            } else if (weaponShops.includes(signType)) {
                color = '#C0392B'; // Dark red for weapons
                icon = 'weapon';
            } else if (serviceShops.includes(signType)) {
                color = '#27AE60'; // Green for services
                icon = 'service';
            } else if (craftShops.includes(signType)) {
                color = '#3498DB'; // Blue for crafts
                icon = 'craft';
            } else {
                // Default shops (Theatre, Library, Shipwright, BarberShop, Bard, Customs, Provisioner)
                color = '#17A2B8'; // Cyan
                icon = 'shop';
            }
        }

        // Override color if has vendors (but keep icon)
        if (hasVendors && !isFocused) {
            color = '#28A745'; // Green when has vendors
        }

        return { color, icon };
    },

    // Draw vendor icon based on type
    drawVendorIcon: function(x, y, size, iconType, isFocused) {
        const strokeColor = isFocused ? '#FFFFFF' : '#000000';
        const lineWidth = isFocused ? 1.5 : 1;
        this.ctx.strokeStyle = strokeColor;
        this.ctx.fillStyle = strokeColor;
        this.ctx.lineWidth = lineWidth;
        const s = size * 0.55;

        switch (iconType) {
            case 'beehive':
                // Hexagonal beehive shape with honey cells
                this.ctx.beginPath();
                for (let i = 0; i < 6; i++) {
                    const angle = (Math.PI / 3) * i - Math.PI / 2;
                    const px = x + s * Math.cos(angle);
                    const py = y + s * Math.sin(angle);
                    if (i === 0) this.ctx.moveTo(px, py);
                    else this.ctx.lineTo(px, py);
                }
                this.ctx.closePath();
                this.ctx.stroke();
                // Inner dot for honey
                this.ctx.beginPath();
                this.ctx.arc(x, y, s * 0.25, 0, 2 * Math.PI);
                this.ctx.fill();
                break;

            case 'guild':
                // Shield/banner shape for guilds
                this.ctx.beginPath();
                this.ctx.moveTo(x - s * 0.6, y - s * 0.7);
                this.ctx.lineTo(x + s * 0.6, y - s * 0.7);
                this.ctx.lineTo(x + s * 0.6, y + s * 0.2);
                this.ctx.lineTo(x, y + s * 0.8);
                this.ctx.lineTo(x - s * 0.6, y + s * 0.2);
                this.ctx.closePath();
                this.ctx.stroke();
                break;

            case 'wooden':
                // Wooden plank sign
                this.ctx.lineWidth = lineWidth * 1.5;
                // Horizontal plank
                this.ctx.beginPath();
                this.ctx.moveTo(x - s * 0.8, y - s * 0.2);
                this.ctx.lineTo(x + s * 0.8, y - s * 0.2);
                this.ctx.lineTo(x + s * 0.8, y + s * 0.3);
                this.ctx.lineTo(x - s * 0.8, y + s * 0.3);
                this.ctx.closePath();
                this.ctx.stroke();
                // Post
                this.ctx.beginPath();
                this.ctx.moveTo(x, y + s * 0.3);
                this.ctx.lineTo(x, y + s * 0.9);
                this.ctx.stroke();
                break;

            case 'brass':
                // Oval brass sign
                this.ctx.lineWidth = lineWidth * 1.5;
                this.ctx.beginPath();
                this.ctx.ellipse(x, y - s * 0.1, s * 0.7, s * 0.45, 0, 0, 2 * Math.PI);
                this.ctx.stroke();
                // Post
                this.ctx.beginPath();
                this.ctx.moveTo(x, y + s * 0.35);
                this.ctx.lineTo(x, y + s * 0.9);
                this.ctx.stroke();
                break;

            case 'food':
                // Mug/cup shape for food/drink
                this.ctx.beginPath();
                // Cup body
                this.ctx.moveTo(x - s * 0.5, y - s * 0.5);
                this.ctx.lineTo(x - s * 0.4, y + s * 0.5);
                this.ctx.lineTo(x + s * 0.4, y + s * 0.5);
                this.ctx.lineTo(x + s * 0.5, y - s * 0.5);
                this.ctx.stroke();
                // Handle
                this.ctx.beginPath();
                this.ctx.arc(x + s * 0.65, y, s * 0.25, -Math.PI / 2, Math.PI / 2);
                this.ctx.stroke();
                break;

            case 'magic':
                // Star shape for magic
                this.ctx.beginPath();
                for (let i = 0; i < 5; i++) {
                    const outerAngle = (Math.PI * 2 / 5) * i - Math.PI / 2;
                    const innerAngle = outerAngle + Math.PI / 5;
                    const outerX = x + s * 0.8 * Math.cos(outerAngle);
                    const outerY = y + s * 0.8 * Math.sin(outerAngle);
                    const innerX = x + s * 0.35 * Math.cos(innerAngle);
                    const innerY = y + s * 0.35 * Math.sin(innerAngle);
                    if (i === 0) this.ctx.moveTo(outerX, outerY);
                    else this.ctx.lineTo(outerX, outerY);
                    this.ctx.lineTo(innerX, innerY);
                }
                this.ctx.closePath();
                this.ctx.stroke();
                break;

            case 'weapon':
                // Crossed swords
                this.ctx.lineWidth = lineWidth * 1.2;
                // Sword 1
                this.ctx.beginPath();
                this.ctx.moveTo(x - s * 0.7, y - s * 0.7);
                this.ctx.lineTo(x + s * 0.7, y + s * 0.7);
                this.ctx.stroke();
                // Sword 2
                this.ctx.beginPath();
                this.ctx.moveTo(x + s * 0.7, y - s * 0.7);
                this.ctx.lineTo(x - s * 0.7, y + s * 0.7);
                this.ctx.stroke();
                break;

            case 'service':
                // Plus/cross for services (healer style)
                this.ctx.lineWidth = lineWidth * 1.5;
                this.ctx.beginPath();
                this.ctx.moveTo(x, y - s * 0.7);
                this.ctx.lineTo(x, y + s * 0.7);
                this.ctx.stroke();
                this.ctx.beginPath();
                this.ctx.moveTo(x - s * 0.7, y);
                this.ctx.lineTo(x + s * 0.7, y);
                this.ctx.stroke();
                break;

            case 'craft':
                // Gear/cog for crafts
                const teeth = 6;
                const innerR = s * 0.35;
                const outerR = s * 0.65;
                this.ctx.beginPath();
                for (let i = 0; i < teeth * 2; i++) {
                    const angle = (Math.PI / teeth) * i;
                    const r = i % 2 === 0 ? outerR : innerR;
                    const px = x + r * Math.cos(angle);
                    const py = y + r * Math.sin(angle);
                    if (i === 0) this.ctx.moveTo(px, py);
                    else this.ctx.lineTo(px, py);
                }
                this.ctx.closePath();
                this.ctx.stroke();
                // Center hole
                this.ctx.beginPath();
                this.ctx.arc(x, y, s * 0.15, 0, 2 * Math.PI);
                this.ctx.stroke();
                break;

            case 'shop':
            case 'sign':
            default:
                // Default signpost
                this.ctx.lineWidth = lineWidth * 1.2;
                // Post
                this.ctx.beginPath();
                this.ctx.moveTo(x, y - s * 0.7);
                this.ctx.lineTo(x, y + s * 0.8);
                this.ctx.stroke();
                // Sign board
                this.ctx.beginPath();
                this.ctx.moveTo(x - s * 0.7, y - s * 0.5);
                this.ctx.lineTo(x + s * 0.7, y - s * 0.5);
                this.ctx.lineTo(x + s * 0.7, y + s * 0.1);
                this.ctx.lineTo(x - s * 0.7, y + s * 0.1);
                this.ctx.closePath();
                this.ctx.stroke();
                break;
        }
    },

    // Vendor Marker methods
    showVendorMarkers: function(markers, selectedSignType) {
        console.log(`🏪 Showing ${markers.length} vendor markers, selected: ${selectedSignType}`);
        this.vendorMarkers = markers;
        this.selectedVendorSignType = selectedSignType || '';
        this.hoveredVendorMarkerIdx = -1;
        this.vendorMarkerTooltipVisible = false;
        if (this.vendorMarkerDwellTimer) {
            clearTimeout(this.vendorMarkerDwellTimer);
            this.vendorMarkerDwellTimer = null;
        }
        this.redrawAll();
    },

    hideVendorMarkers: function() {
        console.log(`🏪 Hiding vendor markers`);
        this.vendorMarkers = null;
        this.hoveredVendorMarkerIdx = -1;
        this.vendorMarkerTooltipVisible = false;
        if (this.vendorMarkerDwellTimer) {
            clearTimeout(this.vendorMarkerDwellTimer);
            this.vendorMarkerDwellTimer = null;
        }
        this.redrawAll();
    },

    updateVendorMarkerHover: function(screenX, screenY) {
        const worldPos = this.screenToWorld(screenX, screenY);
        const newIdx = this.findVendorMarkerAt(worldPos.x, worldPos.y);

        // Clear existing dwell timer
        if (this.vendorMarkerDwellTimer) {
            clearTimeout(this.vendorMarkerDwellTimer);
            this.vendorMarkerDwellTimer = null;
        }

        // If mouse moved to a different marker or off markers
        if (newIdx !== this.hoveredVendorMarkerIdx) {
            this.hoveredVendorMarkerIdx = newIdx;
            this.vendorMarkerTooltipVisible = false;
            this.redrawAll();
        }

        // If hovering over a marker, start dwell timer
        if (newIdx >= 0) {
            this.vendorMarkerTooltipMousePos = { x: screenX, y: screenY };
            this.vendorMarkerDwellTimer = setTimeout(() => {
                if (this.hoveredVendorMarkerIdx === newIdx) {
                    this.vendorMarkerTooltipVisible = true;
                    this.redrawAll();
                }
            }, this.vendorMarkerDwellDelay);
        }
    },

    clearVendorMarkerHover: function() {
        if (this.vendorMarkerDwellTimer) {
            clearTimeout(this.vendorMarkerDwellTimer);
            this.vendorMarkerDwellTimer = null;
        }
        if (this.hoveredVendorMarkerIdx >= 0 || this.vendorMarkerTooltipVisible) {
            this.hoveredVendorMarkerIdx = -1;
            this.vendorMarkerTooltipVisible = false;
            this.redrawAll();
        }
    },

    findVendorMarkerAt: function(worldX, worldY) {
        if (!this.vendorMarkers || !Array.isArray(this.vendorMarkers)) {
            return -1;
        }

        const clickRadius = 15; // Detection radius in world units

        for (let i = 0; i < this.vendorMarkers.length; i++) {
            const marker = this.vendorMarkers[i];
            if (!marker) continue;

            const dx = worldX - marker.x;
            const dy = worldY - marker.y;
            const distSquared = dx * dx + dy * dy;

            if (distSquared <= clickRadius * clickRadius) {
                return i;
            }
        }

        return -1;
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

/**
 * Scrolls the debug panel viewport to the bottom
 */
window.scrollDebugPanel = function() {
    const viewport = document.getElementById('debugViewport');
    if (viewport) {
        viewport.scrollTop = viewport.scrollHeight;
    }
};

console.log('Region drawing function loaded');
