using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace RunJ {
    public class PredictionArrow {
        private static int _predictionIndex = -1;

        public Label arrow;

        public PredictionArrow() {
        }

        private void UpdatePredictionArrow() {
            if (_predictionIndex != -1) {
                arrow.Opacity = 1;
                Canvas.SetTop(arrow, 24.5 + 17.5 * _predictionIndex);
            } else {
                arrow.Opacity = 0;
            }
        }

        public void DecrementPredictionIndex() {
            if (--_predictionIndex < 0)
                _predictionIndex = 3;

            UpdatePredictionArrow();
        }

        public void IncrementPredictionIndex() {
            if (++_predictionIndex > 3)
                _predictionIndex = 0;

            UpdatePredictionArrow();
        }

        /// <summary>
        /// i.e. Hide the arrow
        /// </summary>
        public void ResetArrow() {
            _predictionIndex = -1;

            UpdatePredictionArrow();
        }

        /// <summary>
        /// Get the index of the arrow
        /// </summary>
        public int GetIndex() {
            return _predictionIndex;
        }
    }
}