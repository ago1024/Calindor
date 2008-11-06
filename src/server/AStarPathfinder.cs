/*
 * Copyright (C) 2008 Alexander Gottwald
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 */

/*
 * AStarPathfinder is a replacement to the old pathfinder.
 *
 * It uses the A-Star algorithm to find the shortest path.
 * To avoid time consuming searches for non-existing paths it
 * clusters the map into contiguous areas using a simple flood
 * fill algorithm. The A-Star search is only done for start and
 * end in the same cluster.
 *
 * This implementation of the A-Star search supports a rectangular
 * target area and ends if any spot in the area is reached.
 *
 * The algorithm also simulates the EL server behaviour that hight
 * differences larger then 2 (40 cm) are considered non-walkable
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Calindor.Server.Maps
{
    public class AStarPathfinder : Pathfinder
    {
        /*
         * FloodFill algorithm.
         *
         * Uses stack based 8-way approach and ignores already
         * filled tiles.
         */
        private class FloodFill
        {
            private struct Tile
            {
                public int x;
                public int y;
                public Tile(int x, int y)
                {
                    this.x = x;
                    this.y = y;
                }
            }
            private Stack<Tile> stack = new Stack<Tile>();

            private int sizeX;              /// Map extend x
            private int sizeY;              /// Map extend y
            private byte[,] heightMap;      /// Internal copy of the height map

            private short[,] data;          /// Cluster data
            public short[,] Data            /// Cluster data
            {
                get { return data; }
            }

            /*
             * Process tile.
             */
            private void Push(int x, int y, byte cmp)
            {
                // Check for map and array boundary
                if (x < 0 || x >= sizeX || y < 0 || y >= sizeY)
                    return;

                // Check for already processed tiles
                if (data[x, y] != 0)
                    return;

                // Get the tile height and compare with source tile height
                byte height = heightMap[x, y];
                if (height == 0)
                    return;
                if (height > cmp && height - cmp > 2)
                    return;
                if (height < cmp && cmp - height > 2)
                    return;

                // Add tile to the stack. It's unprocessed and reachable from the neighbour
                stack.Push(new Tile(x, y));
            }

            /*
             * Get the next tile from the stack
             */
            private Tile Pop()
            {
                return (Tile)stack.Pop();
            }

            /*
             * Create FloodFill algorithm.
             */
            public FloodFill(short[,] data, byte[,] heightMap)
            {
                this.sizeX = data.GetLength(0);
                this.sizeY = data.GetLength(1);
                this.data = data;
                this.heightMap = heightMap;
            }

            /*
             * Perform flood fill with color value starting at x,y
             */
            public void Fill(int x, int y, short value)
            {
                stack = new Stack<Tile>();

                // Process the start tile
                Push(x, y, heightMap[x, y]);

                // Continue until no unprocessed tiles are available
                while (stack.Count > 0)
                {
                    // Get the current tile
                    Tile tile = Pop();
                    x = tile.x;
                    y = tile.y;

                    // Mark tile
                    data[x, y] = value;

                    // Process all 8 neighbour tiles
                    byte cmp = heightMap[x, y];
                    Push(x, y + 1, cmp);
                    Push(x, y - 1, cmp);
                    Push(x + 1, y, cmp);
                    Push(x - 1, y, cmp);
                    Push(x + 1, y + 1, cmp);
                    Push(x + 1, y - 1, cmp);
                    Push(x - 1, y + 1, cmp);
                    Push(x - 1, y - 1, cmp);
                }
            }
        }

        /*
         * Key for the map tiles dictionary
         * Map tiles are identified by (x,y) coordinates
         */
        class TileKey
        {
            public short x;         /// x coordinate
            public short y;         /// y coordinate

            public TileKey(short x, short y)
            {
                this.x = x;
                this.y = y;
            }
        }

        /*
         * Search node for the  map tiles
         * This includes the key for easier access
         */
        class TileNode : TileKey
        {
            public int g;               /// real path cost to this node
            public int f;               /// existimated path cost to the target
            public TileNode p = null;   /// predecessor in path

            public TileNode(short x, short y, int g, int f)
                : base(x, y)
            {
                this.g = g;
                this.f = f;
            }
        }

        private Dictionary<TileKey, TileNode> openNodes;
        private Dictionary<TileKey, TileNode> closedNodes;
        private short endx;
        private short endy;
        private short endx2;
        private short endy2;

        private int SizeX;
        private int SizeY;
        private byte[,] heightMap;
        private short[,] clusterData;

        private int clusterCount = 0;
        public int ClusterCount
        {
            get { return clusterCount; }
        }

        /*
         * Create the A-Star path finder object
         */
        public AStarPathfinder(byte[,] heightmap)
            : base(heightmap)
        {
            // Setup
            this.SizeX = heightmap.GetLength(0);
            this.SizeY = heightmap.GetLength(1);
            this.heightMap = heightmap;
            this.clusterData = new short[SizeX, SizeY];

            // Add all non-walkable tiles to cluster 0
            for (int y = 0; y < SizeY; y++)
                for (int x = 0; x < SizeX; x++)
                {
                    // nonwalkable tile
                    if (heightMap[x, y] == 0)
                        clusterData[x, y] = -1;
                    else
                        clusterData[x, y] = 0;
                }

            // Perform a flood fill on all unclustered tiles to discover the clusters
            FloodFill floodFill = new FloodFill(clusterData, heightMap);
            for (int y = 0; y < SizeY; y++)
                for (int x = 0; x < SizeX; x++)
                    if (clusterData[x, y] == 0)
                        floodFill.Fill(x, y, (short)++clusterCount);
        }

        public short GetCluster(int x, int y)
        {
            if (x < 0 || x >= SizeX || y < 0 || y >= SizeY || clusterData == null)
                return -1;
            return clusterData[x, y];
        }

        /*
         * Comparer for the tilenodes.
         * Compares the tile nodes using field 'f', the estimated path cost to target
         */
        class NodeComparer : IComparer<TileNode>
        {
            public int Compare(TileNode x, TileNode y)
            {
                return x.f.CompareTo(y.f);
            }
        }

        /*
         * Helper to compare nodes for equality.
         * Nodes are considered equal if they reference the same tile, so x and y
         * coordinates must be equal
         */
        class NodeEqualityComparer : IEqualityComparer<TileKey>
        {
            public bool Equals(TileKey a, TileKey b)
            {
                return a.x == b.x && a.y == b.y;
            }

            public int GetHashCode(TileKey obj)
            {
                return (int)obj.x << 16 + obj.y;
            }
        }

        /*
         * Find the node with the lowest estimated path costs and remove that node
         * from openNodes
         */
        private TileNode RemoveMin()
        {
            List<TileNode> keys = new List<TileNode>(openNodes.Values);
            keys.Sort(new NodeComparer());
            TileNode key = keys[0];
            openNodes.Remove(key);
            return key;
        }


        /*
         * Examine all neighbours of a node and add them to openNodes unless they
         * are not directly reachable or are already processed
         */
        private void ExpandNode(TileNode currentNode)
        {
            byte height = heightMap[currentNode.x, currentNode.y];
            for (int i = 0; i < 8; i++)
            {
                // Calculate relative position of the neighbour node
                short dx;
                short dy;
                switch (i)
                {
                    case 0:
                        dx = +1; dy = 0;
                        break;
                    case 1:
                        dx = -1; dy = 0;
                        break;
                    case 2:
                        dx = 0; dy = +1;
                        break;
                    case 3:
                        dx = 0; dy = -1;
                        break;
                    case 4:
                        dx = +1; dy = +1;
                        break;
                    case 5:
                        dx = +1; dy = -1;
                        break;
                    case 6:
                        dx = -1; dy = +1;
                        break;
                    case 7:
                        dx = -1; dy = -1;
                        break;
                    default:
                        continue;
                }
                short x = (short)(currentNode.x + dx);
                short y = (short)(currentNode.y + dy);

                if (!IsLocationWalkable(x, y))
                    continue;

                // Check for height differences
                byte nextHeight = heightMap[x, y];
                if (height > nextHeight && height - nextHeight > 2)
                    continue;
                if (height < nextHeight && nextHeight - height > 2)
                    continue;

                // Check if the node has already been processed
                TileNode nextNode = new TileNode(x, y, 0, 0);
                if (closedNodes.ContainsKey(nextNode))
                    continue;

                // Check if the node is alreay in openNodes
                if (openNodes.ContainsKey(nextNode))
                    nextNode = openNodes[nextNode];

                // Calculate direct path costs to the neighbour node
                int c = dx * dx + dy * dy;

                // Calculate estimated path costs
                int h = (x - endx) * (int)(x - endx) + (y - endy) * (int)(y - endy);
                int f = currentNode.g + c + h;

                // Check if the node already has a shorter path */
                if (openNodes.ContainsKey(nextNode) && f > nextNode.f)
                    continue;

                // Update path and path cost for this node
                nextNode.p = currentNode;
                nextNode.f = f;
                nextNode.g = currentNode.g + c;

                // Add the node to openNodes
                if (!openNodes.ContainsKey(nextNode))
                    openNodes.Add(nextNode, nextNode);
            }
        }

        /*
         * Check if the tile is inside the target area
         */
        private bool inEndArea(short x, short y)
        {
            return x >= endx && x < endx2 && y >= endy && y < endy2;
        }

        /*
         * Calculate path
         */
        public override WalkPath CalculatePath(PathfinderParameters _params)
        {
            WalkPath _return = new WalkPath();

            // Check if start is walkable
            if (!IsLocationWalkable(_params.StartX, _params.StartY))
            {
                _return.State = WalkPathState.NON_WALKABLE_START_LOCATION;
                return _return;
            }

            // Check if target is walkable and reachable from start (both are in the same cluster)
            short cluster = GetCluster(_params.StartX, _params.StartY);
            if (!_params.EndIsArea)
            {
                // Handle single tile
                if (!IsLocationWalkable(_params.EndX, _params.EndY))
                {
                    _return.State = WalkPathState.NON_WALKABLE_END_LOCATION;
                    return _return;
                }
                if (GetCluster(_params.EndX, _params.EndY) != cluster)
                {
                    _return.State = WalkPathState.INVALID_NO_PATH_EXISTS;
                    return _return;
                }
            }
            else
            {
                // Handle area
                // Check every tile in the area if any matches the reachable criteria
                bool anyWalkable = false;
                bool anyReachable = false;
                for (short x = _params.EndX; x < _params.EndX2 && !anyWalkable; x++)
                    for (short y = _params.EndY; y < _params.EndY2 && !anyWalkable; y++)
                    {
                        if (IsLocationWalkable(x, y))
                            anyWalkable = true;
                        if (GetCluster(x, y) == cluster)
                            anyReachable = true;
                    }
                if (!anyWalkable)
                {
                    _return.State = WalkPathState.NON_WALKABLE_END_LOCATION;
                    return _return;
                }
                if (!anyReachable)
                {
                    _return.State = WalkPathState.INVALID_NO_PATH_EXISTS;
                    return _return;
                }
            }

            // Prepare end area
            endx = _params.EndX;
            endy = _params.EndY;
            if (_params.EndIsArea)
            {
                endx2 = _params.EndX2;
                endy2 = _params.EndY2;
            }
            else
            {
                endx2 = (short)(_params.EndX + 1);
                endy2 = (short)(_params.EndY + 1);
            }

            // Prepare search
            openNodes = new Dictionary<TileKey, TileNode>(new NodeEqualityComparer());
            closedNodes = new Dictionary<TileKey, TileNode>(new NodeEqualityComparer());

            int f = (_params.StartX - _params.EndX) * (_params.StartX - _params.EndX) + (_params.StartY - _params.EndY) * (_params.StartY - _params.EndY);
            TileNode start = new TileNode(_params.StartX, _params.StartY, 0, f);
            TileNode end = null;

            // Add start nodes
            openNodes.Add(start, start);

            // Process until no node is left in openNodes
            while (openNodes.Count > 0)
            {
                // Get the best node
                TileNode currentNode = RemoveMin();

                // Check if the current node is inside the target area
                if (inEndArea(currentNode.x, currentNode.y))
                {
                    // Stop search
                    closedNodes.Add(currentNode, currentNode);
                    end = currentNode;
                    break;
                }

                // Process the neighbour of the current node
                ExpandNode(currentNode);

                // Add the current node to closed nodes
                closedNodes.Add(currentNode, currentNode);
            }

            // Check if we did not reach the target area
            if (end == null || !closedNodes.ContainsKey(end))
            {
                _return.State = WalkPathState.INVALID_NO_PATH_EXISTS;
                return _return;
            }

            // Walk back the parent nodes from the last node to the start
            Stack<TileNode> stack = new Stack<TileNode>();
            TileNode node = closedNodes[end];
            while (node != null)
            {
                stack.Push(node);
                node = node.p;
            }

            // Check if we reached the start again
            if (stack.Peek().x == _params.StartX && stack.Peek().y == _params.StartY)
            {
                // Create the WalkPath object
                _return.State = WalkPathState.VALID;
                while (stack.Count > 0)
                {
                    node = stack.Pop();
                    _return.AddToPath(new WalkPathItem(node.x, node.y));
                }
            }

            return _return;
        }
    }
}
