using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Types;
using Mapping_Tools_Core.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.Contexts;

namespace Mapping_Tools_Core.BeatmapHelper {
    /// <summary>
    /// Base class for a hit object in osu! stable.
    /// Has extra fields for easier editing and analysis.
    /// </summary>
    public abstract class HitObject : IComparable<HitObject>, IHasPosition, IHasStartTime {
        /// <summary>
        /// Selected hit object.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Base position of hit object.
        /// </summary>
        public Vector2 Pos { get; set; }

        /// <summary>
        /// Absolute time of the hit object in milliseconds.
        /// </summary>
        public double StartTime { get; set; }

        /// <summary>
        /// Hit object forces a new combo.
        /// </summary>
        public bool NewCombo { get; set; }

        /// <summary>
        /// Extra combo skip for combo colours.
        /// By default a new combo adds one to the colour index.
        /// This value adds additional increments to the colour index each new combo.
        /// </summary>
        public int ComboSkip { get; set; }

        /// <summary>
        /// The hitsounds of this hit object.
        /// </summary>
        [NotNull]
        public HitSampleInfo Hitsounds { get; set; }

        /// <summary>
        /// Additional properties.
        /// The objects always have the type of their key.
        /// </summary>
        private Dictionary<Type, IContext> contexts;

        protected HitObject() {
            Hitsounds = new HitSampleInfo();
            contexts = new Dictionary<Type, IContext>();
        }

        /// <summary>
        /// Gets the context with type T.
        /// </summary>
        /// <typeparam name="T">The type to get the context of.</typeparam>
        /// <exception cref="KeyNotFoundException">If the context does not exist in this hit object.</exception>
        /// <returns>The context object with type T.</returns>
        public T GetContext<T>() where T : IContext {
            return (T) contexts[typeof(T)];
        }

        /// <summary>
        /// Tries to get the context with type T.
        /// </summary>
        /// <param name="context">The found context with type T.</param>
        /// <typeparam name="T">The type to get the context of.</typeparam>
        /// <returns>Whether the context exists in this hit object.</returns>
        public bool TryGetContext<T>(out T context) where T : IContext {
            if (contexts.TryGetValue(typeof(T), out var context2)) {
                context = (T) context2;
                return true;
            }

            context = default;
            return false;
        }
        
        /// <summary>
        /// Sets the context object of type T.
        /// </summary>
        /// <typeparam name="T">The context type to set.</typeparam>
        /// <param name="context">The context object to store in this hit object.</param>
        public void SetContext<T>(T context) where T : IContext {
            contexts[typeof(T)] = context;
        }

        /// <summary>
        /// Removes the context of type T from the hit object.
        /// </summary>
        /// <typeparam name="T">The type to remove the context of.</typeparam>
        /// <returns>Whether a context was removed.</returns>
        public bool RemoveContext<T>() where T : IContext {
            return RemoveContext(typeof(T));
        }

        /// <summary>
        /// Removes the context of type T from the hit object.
        /// </summary>
        /// <param name="t">The type to remove the context of.</param>
        /// <returns>Whether a context was removed.</returns>
        public bool RemoveContext(Type t) {
            return contexts.Remove(t);
        }

        /// <summary>
        /// Removes all hitounds and sets samplesets to auto.
        /// Also clears hitsounds from timeline objects and clears body hitsounds.
        /// </summary>
        public virtual void ResetHitsounds() {
            Hitsounds = new HitSampleInfo();
        }

        /// <summary>
        /// </summary>
        /// <param name="deltaTime"></param>
        public virtual void MoveTime(double deltaTime) {
            StartTime += deltaTime;
        }

        /// <summary>
        /// </summary>
        /// <param name="delta"></param>
        public virtual void Move(Vector2 delta) {
            Pos += delta;
        }

        /// <summary>
        /// Apply a 2x2 transformation matrix to the hit object position.
        /// </summary>
        /// <param name="mat">The transformation matrix to apply to the position</param>
        public virtual void Transform(Matrix2 mat) {
            Pos = Matrix2.Mult(mat, Pos);
        }

        public override string ToString() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Makes a deep clone of this hit object and returns it.
        /// </summary>
        /// <returns>The deep clone of this hit object.</returns>
        public HitObject DeepClone() {
            var newHitObject = (HitObject) MemberwiseClone();

            newHitObject.contexts = new Dictionary<Type, IContext>();
            foreach (var (type, context) in contexts) {
                newHitObject.contexts.Add(type, context.Copy());
            }

            // Deep clone for the types inheriting HitObject
            DeepCloneAdd(newHitObject);

            return newHitObject;
        }

        protected abstract void DeepCloneAdd(HitObject clonedHitObject);

        public int CompareTo(HitObject other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            if (StartTime == other.StartTime) return other.NewCombo.CompareTo(NewCombo);
            return StartTime.CompareTo(other.StartTime);
        }
    }
}