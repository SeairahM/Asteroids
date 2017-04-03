﻿using System;
using System.Collections.Generic;
using SFML.System;
using SFML.Window;
using System.Drawing;
using SFML.Graphics;
using System.IO;

namespace Asteroids
{
    class Asteroids : Game
    {
        private Ship playerShip;

        private Dictionary<string, Projectile> dictProjectiles;
        private Dictionary<string, Asteroid> dictAsteroids;

        private HashSet<string> projectileDeletions;
        private HashSet<string> asteroidDeletions;

        // Variables relevant to spawning asteroids
        private HashSet<Asteroid> brokenParentAsteroids;

        private Random rnd;
        private Array edgeArray = Enum.GetValues(typeof(Edge));
        private const double SPAWN_CHANCE = .10; // Chance that a asteroid spawns
        public const int MIN_ASTEROID_SIZE = 15;
        public const int MAX_ASTEROID_SIZE = 50;
        public const int MAX_UNSCALED_ASTEROID_SPEED = 25;
        // The result of floor(score, SPAWN_SCALE_FACTOR) decides how many asteroids are on the screen at once 
        private int score; 
        private const float SPAWN_SCALE_FACTOR = 2;
        // Text for score board
        private Text scoreText;

        public Asteroids(uint width, uint height, string title, Color clrColor) : base(width, height, title, clrColor)
        {
            // Initialize Random
            rnd = new Random();

            dictAsteroids = new Dictionary<string, Asteroid>();
            dictProjectiles = new Dictionary<string, Projectile>();

            projectileDeletions = new HashSet<string>();
            asteroidDeletions = new HashSet<string>();

            brokenParentAsteroids = new HashSet<Asteroid>();

            // Setup the score text
            scoreText = new Text("Score: 0", font);
            scoreText.CharacterSize = FONT_SIZE;
            scoreText.Color = Color.White;

        }

        public override void CleanUp()
        {
            // Clear all data containers
            dictAsteroids.Clear();
            dictProjectiles.Clear();

            projectileDeletions.Clear();
            asteroidDeletions.Clear();

            brokenParentAsteroids.Clear();

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
            SpawningPhase();
            CollisionChecks();
            DeletionPhase();
            UpdateAndDrawPhase();
        }
        /// <summary>
        /// Checks for collision between entities
        /// 1. For player-asteroid collision game ends
        /// 2. For projectile-asteroid collision, score is incremented
        ///     and projectile and asteroid are added to deletion hashsets
        /// </summary>
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
                    if (p.IsExpired) projectileDeletions.Add(p.Id);
                    else if (a.ShouldExplode(p))
                    {
                        asteroidDeletions.Add(a.Id);
                        projectileDeletions.Add(p.Id);
                        if (a.WillBreakApart()) brokenParentAsteroids.Add(a);
                        score++;
                    }
                }
                
            }
        }
        /// <summary>
        /// Method that goes through deletion hashsets and removes entities from entity dictionaries
        /// NOTE: Seperate deletion phase to avoid deleting entities during iteration 
        /// of collision checks
        /// </summary>
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
            // To avoid longer iteration times on subsequent deletion phases
            projectileDeletions.Clear();
            asteroidDeletions.Clear();
        }
        /// <summary>
        /// Method that updates and draws all relevant entities onto the screen
        /// </summary>
        private void UpdateAndDrawPhase()
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
            UpdateScore();
            window.Draw(scoreText);
        }
        /// <summary>
        /// Method that checks whether it should spawn on asteroid based on the score
        /// and scale factor. If it should, then it spawns a new asteroid.
        /// </summary>
        private void SpawningPhase()
        {
            foreach(Asteroid a in brokenParentAsteroids)
            {
                // Asteroid will break into two smaller asteroids
                Asteroid a1, a2;
                a1 = SpawnAsteroid(a.getCenterVertex(),(int) a.Radius/2);
                a2 = SpawnAsteroid(a.getCenterVertex(), (int)a.Radius / 2);
                dictAsteroids.Add(a1.Id, a1);
                dictAsteroids.Add(a2.Id, a2);
            }
            // Reset the hashset since everything has been taken care of
            brokenParentAsteroids.Clear();

            if (dictAsteroids.Count <= score/SPAWN_SCALE_FACTOR)
            {
                // Random chance to spawn
                if(rnd.NextDouble() < SPAWN_CHANCE)
                {
                    // Get a random edge to spawn Asteroid at
                    Edge randomEdge = (Edge)edgeArray.GetValue(rnd.Next(edgeArray.Length));
                    if (randomEdge != Edge.NULL)
                    {
                        Asteroid newAsteroid = SpawnAsteroid(randomEdge);
                        dictAsteroids.Add(newAsteroid.Id, newAsteroid);
                    };
                }
            };
        }
        /// <summary>
        /// Spawns an asteroid with a random position and velocity at the 
        /// edges of the screen
        /// </summary>
        /// <param name="window"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        private Asteroid SpawnAsteroid(Edge edge)
        {
            // I have to initalize variables?
            float xPos, yPos, xVel, yVel;
            int asteroidRadius;
            xPos = 0; yPos = 0; xVel = 0; yVel = 0;
            // TODO - Figure out how to clean this up...
            switch (edge)
            {
                case Edge.LEFT:
                    xPos = 0 - MAX_ASTEROID_SIZE;
                    yPos = rnd.Next(0, (int) window.Size.Y);
                    xVel = rnd.Next(0, MAX_UNSCALED_ASTEROID_SPEED);
                    yVel = rnd.Next(-MAX_UNSCALED_ASTEROID_SPEED, MAX_UNSCALED_ASTEROID_SPEED);
                    break;
                case Edge.RIGHT:
                    xPos = window.Size.X + MAX_ASTEROID_SIZE;
                    yPos = rnd.Next(0, (int) window.Size.Y);
                    xVel = rnd.Next(-MAX_UNSCALED_ASTEROID_SPEED, 0);
                    yVel = rnd.Next(-MAX_UNSCALED_ASTEROID_SPEED, MAX_UNSCALED_ASTEROID_SPEED);
                    break;
                case Edge.UP:
                    xPos = rnd.Next(0, (int) window.Size.X);
                    yPos = 0 - MAX_ASTEROID_SIZE;
                    xVel = rnd.Next(-MAX_UNSCALED_ASTEROID_SPEED, MAX_UNSCALED_ASTEROID_SPEED);
                    yVel = rnd.Next(0, MAX_UNSCALED_ASTEROID_SPEED);
                    break;
                case Edge.DOWN:
                    xPos = rnd.Next(0, (int) window.Size.X);
                    yPos = window.Size.Y + MAX_ASTEROID_SIZE;
                    xVel = rnd.Next(-MAX_UNSCALED_ASTEROID_SPEED, MAX_UNSCALED_ASTEROID_SPEED);
                    yVel = rnd.Next(-MAX_UNSCALED_ASTEROID_SPEED, 0);
                    break;
            }
            Vector2f p = new Vector2f(xPos, yPos);
            Vector2f v = new Vector2f(xVel, yVel);
            asteroidRadius = rnd.Next(MIN_ASTEROID_SIZE, MAX_ASTEROID_SIZE);
            return new Asteroid(p, v, asteroidRadius);
        }
        /// <summary>
        /// Spawns an asteroid with random velocity and random size 
        /// (Bounded by a given max radius) at a given position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Asteroid SpawnAsteroid(Vector2f pos, int maxRadius)
        {
            float xVel, yVel;
            xVel = rnd.Next(-MAX_UNSCALED_ASTEROID_SPEED, MAX_UNSCALED_ASTEROID_SPEED);
            yVel = rnd.Next(-MAX_UNSCALED_ASTEROID_SPEED, MAX_UNSCALED_ASTEROID_SPEED);
            Vector2f v = new Vector2f(xVel, yVel);
            return new Asteroid(pos, v, rnd.Next(MIN_ASTEROID_SIZE, maxRadius));
        }
        private void UpdateScore()
        {
            scoreText.DisplayedString = "Score: " + score.ToString();
        }
    }
}
