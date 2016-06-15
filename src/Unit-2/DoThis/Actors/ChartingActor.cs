using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ChartingActor : ReceiveActor, IWithUnboundedStash
    {
        public const int MaxPoints = 250;

        private readonly Chart _chart;
        private readonly Button pauseButton;
        private Dictionary<string, Series> _seriesIndex;

        private int xPosCounter = 0;

        public ChartingActor(Chart chart, Button pauseButton) : this(chart, new Dictionary<string, Series>(), pauseButton)
        {
        }

        private ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex, Button pauseButton)
        {
            _chart = chart;
            _seriesIndex = seriesIndex;
            this.pauseButton = pauseButton;

            Receive<AddSeries>(addSeries => HandleAddSeries(addSeries));
            Receive<InitializeChart>(x => HandleInitialize(x));
            Receive<RemoveSeries>(x => HandleRemoveSeries(x));
            Receive<ChartingMessages.Metric>(x => HandleMetrics(x));

            Receive<ChartingMessages.TogglePaused>(x =>
            {
                SetPausedButtonText(true);
                BecomeStacked(Paused);
            });
        }

        /// <summary>
        /// Gets or sets the stash. This will be automatically populated by the framework AFTER the constructor has been run.
        ///             Implement this as an auto property.
        /// </summary>
        /// <value>
        /// The stash.
        /// </value>
        public IStash Stash { get; set; }

        private void HandleMetricPaused(ChartingMessages.Metric metric)
        {
            if (string.IsNullOrEmpty(metric.Series) || _seriesIndex.ContainsKey(metric.Series) == false)
            {
                return;
            }
            Series series = _seriesIndex[metric.Series];
            series.Points.AddXY(xPosCounter++, 0.0d);
            while (series.Points.Count > MaxPoints)
            {
                series.Points.RemoveAt(0);
            }
            SetChartBoundaries();
        }

        private void HandleMetrics(ChartingMessages.Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) &&
                _seriesIndex.ContainsKey(metric.Series))
            {
                Series series = _seriesIndex[metric.Series];
                series.Points.AddXY(xPosCounter++, metric.CounterValue);
                while (series.Points.Count > MaxPoints)
                {
                    series.Points.RemoveAt(0);
                }
                SetChartBoundaries();
            }
        }

        private void HandleRemoveSeries(RemoveSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series) &&
                _seriesIndex.ContainsKey(series.Series))
            {
                Series seriesToRemove = _seriesIndex[series.Series];
                _seriesIndex.Remove(series.Series);
                _chart.Series.Remove(seriesToRemove);
                SetChartBoundaries();
            }
        }

        private void Paused()
        {
            Receive<AddSeries>(x => Stash.Stash());
            Receive<RemoveSeries>(x => Stash.Stash());
            Receive<ChartingMessages.Metric>(x => HandleMetricPaused(x));
            Receive<ChartingMessages.TogglePaused>(x =>
            {
                SetPausedButtonText(false);
                UnbecomeStacked();
                Stash.UnstashAll();
            });
        }

        private void SetChartBoundaries()
        {
            double maxAxisX, maxAxisY, minAxisX, minAxisY = 0.0d;
            List<DataPoint> allPoints = _seriesIndex.Values.SelectMany(series => series.Points).ToList();
            List<double> yValues = allPoints.SelectMany(point => point.YValues).ToList();
            maxAxisX = xPosCounter;
            minAxisX = xPosCounter - MaxPoints;
            maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1.0d;
            minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0.0d;
            if (allPoints.Count > 2)
            {
                ChartArea area = _chart.ChartAreas[0];
                area.AxisX.Minimum = minAxisX;
                area.AxisX.Maximum = maxAxisX;
                area.AxisY.Minimum = minAxisY;
                area.AxisY.Maximum = maxAxisY;
            }
        }

        private void SetPausedButtonText(bool paused)
        {
            pauseButton.Text = !paused ? "PAUSED ||" : "RESUME > ";
            ;
        }

        #region Messages

        public class InitializeChart
        {
            public InitializeChart(Dictionary<string, Series> initialSeries)
            {
                InitialSeries = initialSeries;
            }

            public Dictionary<string, Series> InitialSeries { get; private set; }
        }

        public class AddSeries
        {
            public AddSeries(Series series)
            {
                Series = series;
            }

            public Series Series { get; set; }
        }

        public class RemoveSeries
        {
            public RemoveSeries(string Series)
            {
                this.Series = Series;
            }

            public string Series { get; set; }
        }

        #endregion

        //        protected override void OnReceive(object message)
        //        {
        //            if (message is InitializeChart)
        //            {
        //                var ic = message as InitializeChart;
        //                HandleInitialize(ic);
        //            }
        //        }

        #region Individual Message Type Handlers

        private void HandleAddSeries(AddSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series.Name) &&
                !_seriesIndex.ContainsKey(series.Series.Name))
            {
                _seriesIndex.Add(series.Series.Name, series.Series);
                _chart.Series.Add(series.Series);
                SetChartBoundaries();
            }
        }

        private void HandleInitialize(InitializeChart ic)
        {
            if (ic.InitialSeries != null)
            {
                // swap the two series out
                _seriesIndex = ic.InitialSeries;
            }

            // delete any existing series
            _chart.Series.Clear();

            // set the axes up
            ChartArea area = _chart.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;

            SetChartBoundaries();

            // attempt to render the initial chart
            if (_seriesIndex.Any())
            {
                foreach (KeyValuePair<string, Series> series in _seriesIndex)
                {
                    // force both the chart and the internal index to use the same names
                    series.Value.Name = series.Key;
                    _chart.Series.Add(series.Value);
                }
            }

            SetChartBoundaries();
        }

        #endregion
    }
}
