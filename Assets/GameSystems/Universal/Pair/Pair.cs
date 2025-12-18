using System;
using System.Collections.Generic;
using UnityEngine;

namespace Universal
{
    [Serializable]
    public class Pair<T, U> : IEquatable<Pair<T, U>>
    {
        [field: SerializeField] public T First { get; set; }
        [field: SerializeField] public U Second { get; set; }

        public Pair()
        {
            First = default(T);
            Second = default(U);
        }

        public Pair(T first, U second)
        {
            this.First = first;
            this.Second = second;
        }

        /// <summary>
        /// Checks if two Pair objects are equal based on their content (First and Second).
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as Pair<T, U>);
        }

        /// <summary>
        /// Implements IEquatable<T> for strong-typed comparison.
        /// </summary>
        public bool Equals(Pair<T, U> other)
        {
            if (other == null)
                return false;

            // Uses EqualityComparer<T>.Default to handle null values and custom equality for types.
            return EqualityComparer<T>.Default.Equals(First, other.First) &&
                   EqualityComparer<U>.Default.Equals(Second, other.Second);
        }

        /// <summary>
        /// Generates a hash code based on the combined hash codes of First and Second.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked // Allows overflow checks to be disabled for performance
            {
                int hash = 17;
                hash = hash * 23 + (First == null ? 0 : First.GetHashCode());
                hash = hash * 23 + (Second == null ? 0 : Second.GetHashCode());
                return hash;
            }
        }

        public static bool operator ==(Pair<T, U> left, Pair<T, U> right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public static bool operator !=(Pair<T, U> left, Pair<T, U> right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"[{First}, {Second}]";
        }

    }
}