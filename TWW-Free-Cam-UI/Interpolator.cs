using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWW_Free_Cam_UI.Dolphin;

namespace TWW_Free_Cam_UI
{
    public class cXyz
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public cXyz()
        {

        }
        public cXyz(cXyz other)
        {
            x = other.x;
            y = other.y;
            z = other.z;
        }

        public void SetTo(cXyz other)
        {
            x = other.x;
            y = other.y;
            z = other.z;
        }

    }
    public static class Interpolator
    {
        // Define a delegate to represent an easing function.
        public delegate float EasingFunction(float t);

        /// <summary>
        /// Linear interpolation (no easing).
        /// </summary>
        public static float Linear(float t)
        {
            return t;
        }

        /// <summary>
        /// Ease-In/Ease-Out interpolation using a smoothstep function.
        /// Produces smooth acceleration and deceleration.
        /// Formula: 3t² - 2t³
        /// </summary>
        public static float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }
        public static float EaseIn(float t)
        {
            // Quadratic Ease In: starts slowly and accelerates.
            return t * t;
        }

        public static float EaseOut(float t)
        {
            // Quadratic Ease Out: starts fast and decelerates.
            return 1f - (1f - t) * (1f - t);
        }
        /// <summary>
        /// Returns a cubic Bezier easing function.
        /// The Bezier curve is defined with fixed endpoints 0 and 1 and control points p1 and p2.
        /// p1 and p2 should be values between 0 and 1.
        /// Formula: B(t) = 3*(1-t)²*t*p1 + 3*(1-t)*t²*p2 + t³
        /// </summary>
        public static EasingFunction CubicBezier(float p1, float p2)
        {
            return t =>
            {
                return 3f * (1f - t) * (1f - t) * t * p1 +
                       3f * (1f - t) * t * t * p2 +
                       t * t * t;
            };
        }

        /// <summary>
        /// Interpolates between two cXyz positions given a current step, total steps, and an easing function.
        /// </summary>
        /// <param name="start">The start position.</param>
        /// <param name="end">The end position.</param>
        /// <param name="currentStep">The current step in the transition.</param>
        /// <param name="totalSteps">The total number of steps.</param>
        /// <param name="easing">An easing function mapping a 0-1 value to a new 0-1 value.</param>
        /// <returns>A new cXyz representing the interpolated position.</returns>
        public static cXyz Interpolate(cXyz start, cXyz end, int currentStep, int totalSteps, EasingFunction easing)
        {
            if (totalSteps <= 0)
                throw new ArgumentException("Total steps must be greater than zero.", nameof(totalSteps));

            // Clamp currentStep to [0, totalSteps].
            currentStep = Math.Max(0, Math.Min(currentStep, totalSteps));

            // Compute the base interpolation factor.
            float t = (float)currentStep / totalSteps;
            // Apply the easing function.
            float easedT = easing(t);

            return new cXyz
            {
                x = start.x + (end.x - start.x) * easedT,
                y = start.y + (end.y - start.y) * easedT,
                z = start.z + (end.z - start.z) * easedT
            };
        }
    }
}
