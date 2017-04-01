﻿using System;
using System.Collections.Generic;
using SFML.System;
using SFML.Window;
using SFML.Graphics;

namespace Asteroids
{
    static class Constants
    {
        public const int MAX_ASTEROID_SIZE = 50;
    }
    class Asteroids : Game
    {
        private Ship playerShip;
        private Dictionary<string, Projectile> dictProjectiles;
        private Dictionary<string, Asteroid> dictAsteroids;

        private HashSet<string> projectileDeletions;
        private HashSet<string> asteroidDeletions;

        // Random object
        private Random rnd;
        private Array edgeArray = Enum.GetValues(typeof(Edge));
        private int score;

        public Asteroids(uint width, uint height, string title, Color clrColor) : base(width, height, title, clrColor)
        {
            // Initialize Random
            rnd = new Random();

            dictAsteroids = new Dictionary<string, Asteroid>();
            dictProjectiles = new Dictionary<string, Projectile>();

            projectileDeletions = new HashSet<string>();
            asteroidDeletions = new HashSet<string>();
        }

        public override void CleanUp()
        {
            // Clear all data containers
            dictAsteroids.Clear();
            dictProjectiles.Clear();

            projectileDeletions.Clear();
            asteroidDeletions.Clear();

            score = 0;
            playerShip = null;
        }

        public override void Init()
        {
            playerShip = new Ship(new Vector2f(window.Size.X / 2, window.Size.Y / 2), 20);

            Console.WriteLine("Asteroids started!");
        }

        public override void Restart()
        {
            Console.WriteLine("Press enter when you are ready to restart");
            Console.Out.Flush();
            Console.ReadLine();
            CleanUp();
            Init();
        }

        public override void Update(RenderWindow window, float dt)
        {
            SpawnCheck();
            CollisionChecks();
            DeletionPhase();
            UpdateAndDraw();
        }
        private void CollisionChecks()
        {
            // For loops to check for collisions between everything
            foreach (Asteroid a in dictAsteroids.Values)
            {
                // Check asteroid collision with ship
                // If true then restart game
                if (a.HasCollided(playerShip))
                {
                    Restart();
                    return;
                }
                // Check asteroid collision with ship projectiles
                foreach (Projectile p in dictProjectiles.Values)
                {
                    // Add any deletions to deletion sets
                    if (p.IsExpired) projectileDeletions.Add(p.GetId);
                    else if (a.ShouldExplode(p))
                    {
                        asteroidDeletions.Add(a.GetId);
                        projectileDeletions.Add(p.GetId);
                        score++;
                    }
                }
                
            }
        }
        private void DeletionPhase()
        {
            foreach (string pId in projectileDeletions)
            {
                dictProjectiles.Remove(pId);
            }
            foreach (string aId in asteroidDeletions)
            {
                dictAsteroids.Remove(aId);
            }

        }
        private void UpdateAndDraw()
        {
            playerShip.Update(dt);
            // Check if player can shoot && wants to shoot
            if (playerShip.WantsToShoot && playerShip.IsShotCharged) playerShip.Shoot(dictProjectiles);
            playerShip.Draw(window);

            foreach (Asteroid a in dictAsteroids.Values)
            {
                a.Update(dt);
                a.Draw(window);
            }
            foreach (Projectile p in dictProjectiles.Values)
            {
                p.Update(dt);
                p.Draw(window);
            }
        }
        private void SpawnCheck()
        {
            if (dictAsteroids.Count <= score)
            {
                // Get a random edge to spawn Asteroid at
                Edge randomEdge = (Edge)edgeArray.GetValue(rnd.Next(edgeArray.Length));
                if (randomEdge != Edge.NULL) {
                    Asteroid newAsteroid = SpawnAsteroid(randomEdge);
                    dictAsteroids.Add(newAsteroid.GetId, newAsteroid);
                };
            };
        }
        private Asteroid SpawnAsteroid(Edge edge)
        {
            // I have to initalize variables?
            float xPos, yPos, xVel, yVel;
            xPos = 0; yPos = 0; xVel = 0; yVel = 0;
            switch (edge)
            {
                case Edge.LEFT:
                    xPos = 0 - Constants.MAX_ASTEROID_SIZE;
                    yPos = rnd.Next(0, 800);
                    xVel = rnd.Next(0, 10);
                    yVel = rnd.Next(-10, 10);
                    break;
                case Edge.RIGHT:
                    xPos = 800 + Constants.MAX_ASTEROID_SIZE;
                    yPos = rnd.Next(0, 800);
                    xVel = rnd.Next(-10, 0);
                    yVel = rnd.Next(-10, 10);
                    break;
                case Edge.UP:
                    xPos = rnd.Next(0, 800);
                    yPos = 0 - Constants.MAX_ASTEROID_SIZE;
                    xVel = rnd.Next(-10, 10);
                    yVel = rnd.Next(0, 10);
                    break;
                case Edge.DOWN:
                    xPos = rnd.Next(0, 800);
                    yPos = 800 + Constants.MAX_ASTEROID_SIZE;
                    xVel = rnd.Next(-10, 10);
                    yVel = rnd.Next(-10, 0);
                    break;
            }
            Vector2f p = new Vector2f(xPos, yPos);
            Vector2f v = new Vector2f(xVel, yVel);
            return new Asteroid(p, v);
        }
    }
}
