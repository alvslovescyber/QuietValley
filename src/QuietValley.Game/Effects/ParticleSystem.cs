using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace QuietValley.Game.Effects;

public sealed class ParticleSystem
{
    private readonly List<Particle> _particles = [];
    private readonly Random _random = new();

    public void SpawnDust(Vector2 feetPosition)
    {
        for (int index = 0; index < 2; index++)
        {
            float xOffset = _random.NextSingle() * 8f - 4f;
            _particles.Add(
                new Particle(
                    feetPosition + new Vector2(xOffset, 0),
                    new Vector2(_random.NextSingle() * 10f - 5f, -8f - _random.NextSingle() * 8f),
                    0.38f,
                    new Color(214, 166, 86, 130),
                    2
                )
            );
        }
    }

    public void SpawnPickup(Vector2 position)
    {
        for (int index = 0; index < 6; index++)
        {
            _particles.Add(
                new Particle(
                    position,
                    new Vector2(_random.NextSingle() * 36f - 18f, -18f - _random.NextSingle() * 20f),
                    0.55f,
                    new Color(255, 224, 88, 190),
                    2
                )
            );
        }
    }

    public void Update(float deltaSeconds)
    {
        for (int index = _particles.Count - 1; index >= 0; index--)
        {
            Particle particle = _particles[index];
            particle.Position += particle.Velocity * deltaSeconds;
            particle.Velocity *= 0.88f;
            particle.Lifetime -= deltaSeconds;
            if (particle.Lifetime <= 0)
            {
                _particles.RemoveAt(index);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Vector2 camera)
    {
        foreach (Particle particle in _particles)
        {
            float alpha = Math.Clamp(particle.Lifetime / 0.55f, 0f, 1f);
            Color color = particle.Color * alpha;
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    (int)(particle.Position.X - camera.X),
                    (int)(particle.Position.Y - camera.Y),
                    particle.Size,
                    particle.Size
                ),
                color
            );
        }
    }

    private sealed class Particle(Vector2 position, Vector2 velocity, float lifetime, Color color, int size)
    {
        public Vector2 Position = position;
        public Vector2 Velocity = velocity;
        public float Lifetime = lifetime;
        public Color Color = color;
        public int Size = size;
    }
}
