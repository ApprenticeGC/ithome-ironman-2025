using GameConsole.ECS.Core;
using GameConsole.Engine.Core;
using Xunit;

namespace GameConsole.ECS.Core.Tests;

// Test components for ECS testing
public struct PositionComponent : IComponent
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public PositionComponent(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public struct VelocityComponent : IComponent
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public VelocityComponent(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public struct HealthComponent : IComponent
{
    public int Health { get; set; }
    public int MaxHealth { get; set; }

    public HealthComponent(int health, int maxHealth)
    {
        Health = health;
        MaxHealth = maxHealth;
    }
}

// Test system for ECS testing
public class MovementSystem : ISystem
{
    public UpdatePriority Priority => UpdatePriority.Normal;
    public bool IsEnabled { get; set; } = true;

    public void Update(IECSWorld world, float deltaTime)
    {
        foreach (var entity in world.Query<PositionComponent, VelocityComponent>())
        {
            var position = world.GetComponent<PositionComponent>(entity);
            var velocity = world.GetComponent<VelocityComponent>(entity);

            if (position.HasValue && velocity.HasValue)
            {
                var pos = position.Value;
                var vel = velocity.Value;
                
                pos.X += vel.X * deltaTime;
                pos.Y += vel.Y * deltaTime;
                pos.Z += vel.Z * deltaTime;

                world.UpdateComponent(entity, pos);
            }
        }
    }
}