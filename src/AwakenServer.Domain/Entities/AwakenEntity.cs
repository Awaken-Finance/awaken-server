using System;
using Orleans;
using Volo.Abp.Domain.Entities;

namespace AwakenServer.Entities
{
    /// <inheritdoc cref="IEntity{TKey}" />
    [Serializable]
    public abstract class AwakenEntity<TKey> : Entity, IEntity<TKey>
    {
        /// <inheritdoc/>
        public virtual TKey Id { get; set; }

        protected AwakenEntity()
        {

        }

        protected AwakenEntity(TKey id)
        {
            Id = id;
        }

        public override object[] GetKeys()
        {
            return new object[] {Id};
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[ENTITY: {GetType().Name}] Id = {Id}";
        }
    }
}