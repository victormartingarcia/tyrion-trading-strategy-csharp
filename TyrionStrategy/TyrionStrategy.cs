using System;
using System.Collections.Generic;
using TradingMotion.SDK.Algorithms;
using TradingMotion.SDK.Algorithms.InputParameters;
using TradingMotion.SDK.Markets.Charts;
using TradingMotion.SDK.Markets.Indicators.Momentum;
using TradingMotion.SDK.Markets.Indicators.StatisticFunctions;

using TradingMotion.SDK.Markets.Orders;

/// <summary>
/// Tyrion trading rules:
///   * Entry: Price breaks Stochastic %D level
///   * Exit: Sets a Take Profit (objective) order based on price standard deviation
///   * Filters: None
/// </summary>
namespace TyrionStrategy
{
    public class TyrionStrategy : Strategy
    {
        StochasticIndicator stochasticIndicator;
        StdDevIndicator stdDevIndicator;

        Order limitTakeProfitOrder;

        public TyrionStrategy(Chart mainChart, List<Chart> secondaryCharts)
            : base(mainChart, secondaryCharts)
        {

        }

        /// <summary>
        /// Strategy Name
        /// </summary>
        /// <returns>The complete name of the strategy</returns>
        public override string Name
        {
            get
            {
                return "Tyrion Strategy";
            }
        }

        /// <summary>
        /// Security filter that ensures the Position will be closed at the end of the trading session.
        /// </summary>
        public override bool ForceCloseIntradayPosition
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Security filter that sets a maximum open position size of 1 contract (either side)
        /// </summary>
        public override uint MaxOpenPosition
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// This strategy uses the Advanced Order Management mode
        /// </summary>
        public override bool UsesAdvancedOrderManagement
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Strategy Parameter definition
        /// </summary>
        public override InputParameterList SetInputParameters()
        {
            InputParameterList parameters = new InputParameterList();

            // The previous N bars period StdDev indicator will use
            parameters.Add(new InputParameter("StdDev Period", 20));
            // The number of deviations StdDev indicator will use
            parameters.Add(new InputParameter("StdDev Number of Deviations", 3));

            // The previous N bars period Stochastic indicator will use
            parameters.Add(new InputParameter("Stochastic Period", 77));

            // Break level of Stochastic's %D indicator we consider a buy signal
            parameters.Add(new InputParameter("Stochastic %D Buy signal trigger level", 51));

            return parameters;
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        public override void OnInitialize()
        {
            log.Debug("Tyrion onInitialize()");

            // Adding StdDev indicator to strategy 
            // (see http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:standard_deviation)
            stdDevIndicator = new StdDevIndicator(Bars.Close, (int)this.GetInputParameter("StdDev Period"), (int)this.GetInputParameter("StdDev Number of Deviations"));
            this.AddIndicator("Std Dev indicator", stdDevIndicator);

            // Adding Stochastic indicator to strategy 
            // (see http://stockcharts.com/help/doku.php?id=chart_school:technical_indicators:stochastic_oscillato)
            stochasticIndicator = new StochasticIndicator(Bars.Bars, (int)this.GetInputParameter("Stochastic Period"));
            this.AddIndicator("Stochastic", stochasticIndicator);
        }

        /// <summary>
        /// Strategy enter/exit/filtering rules
        /// </summary>
        public override void OnNewBar()
        {

            if (this.GetOpenPosition() == 0)
            {
                int buySignal = (int)this.GetInputParameter("Stochastic %D Buy signal trigger level");

                if (stochasticIndicator.GetD()[1] <= buySignal && stochasticIndicator.GetD()[0] > buySignal)
                {
                    Order buyOrder = new MarketOrder(OrderSide.Buy, 1, "Entry long");
                    limitTakeProfitOrder = new LimitOrder(OrderSide.Sell, 1, Bars.Close[0] + stdDevIndicator.GetStdDev()[0], "Exit long (take profit stop)");

                    this.InsertOrder(buyOrder);
                    this.InsertOrder(limitTakeProfitOrder);
                }
            }
        }
    }
}
