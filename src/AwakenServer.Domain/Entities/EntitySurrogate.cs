// using System;
// using Orleans;
// using Volo.Abp.Domain.Entities;
//
// namespace AwakenServer.Entities;
//
// // Surrogate for the Entity class
// [GenerateSerializer]
// public struct EntitySurrogate
// {
// }
//
// // Surrogate for the Entity<TKey> class
// [GenerateSerializer]
// public struct EntitySurrogate<TKey>
// {
//     [Id(0)]
//     public TKey Id;
// }
//
// [RegisterConverter]
// public sealed class EntitySurrogateConverter : IConverter<Entity, EntitySurrogate>
// {
//     public Entity ConvertFromSurrogate(in EntitySurrogate surrogate)
//     {
//         throw new NotImplementedException("Entity is abstract and needs a concrete type to instantiate.");
//     }
//
//     public EntitySurrogate ConvertToSurrogate(in Entity entity)
//     {
//         return new EntitySurrogate
//         {
//         };
//     }
//     
//     public void Populate(
//         in EntitySurrogate surrogate, Entity value)
//     {
//     }
// }
//
// [RegisterConverter]
// public sealed class EntitySurrogateConverter<TKey> : IConverter<Entity<TKey>, EntitySurrogate<TKey>>
// {
//     public Entity<TKey> ConvertFromSurrogate(in EntitySurrogate<TKey> surrogate)
//     {
//         return Activator.CreateInstance(typeof(Entity<TKey>), surrogate.Id) as Entity<TKey>;
//     }
//
//     public EntitySurrogate<TKey> ConvertToSurrogate(in Entity<TKey> entity)
//     {
//         return new EntitySurrogate<TKey>
//         {
//             Id = entity.Id
//         };
//     }
//     
//     public void Populate(
//         in EntitySurrogate<TKey> surrogate, Entity<TKey> value)
//     {
//         typeof(Entity<TKey>).BaseType.GetProperty("Id").SetValue(this, surrogate.Id);
//     }
// }
