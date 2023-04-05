{
 "cells": [
  {
   "cell_type": "markdown",
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "source": [
    "# Signal Portfolio - Data Analytics for Finance\n",
    "\n",
    "**Signal Name (e.g., Book to Market):** Gross Profit scaled by Assets (gp_at)"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [
    {
     "data": {
      "text/plain": [
       "Microsoft.DotNet.Interactive.InstallPackagesMessage\n"
      ]
     },
     "metadata": {},
     "output_type": "display_data"
    }
   ],
   "source": [
    "//Load Libraries\n",
    "\n",
    "#r \"nuget: FSharp.Data\"\n",
    "#r \"nuget: FSharp.Stats\"\n",
    "#r \"nuget: Plotly.NET,2.0.0-preview.17\"\n",
    "#r \"nuget: Plotly.NET.Interactive,2.0.0-preview.17\"\n",
    "#r \"nuget: DiffSharp-lite\"\n",
    "#r \"nuget: Accord\"\n",
    "#r \"nuget: Accord.Statistics\"\n",
    "\n",
    "#load \"Portfolio.fsx\"\n",
    "#load \"Common.fsx\"\n",
    "#load \"YahooFinance.fsx\"\n",
    "\n",
    "open DiffSharp\n",
    "open System\n",
    "open FSharp.Data\n",
    "open FSharp.Stats\n",
    "open Plotly.NET\n",
    "open Portfolio\n",
    "open Common\n",
    "open Accord\n",
    "open Accord.Statistics.Models.Regression.Linear\n",
    "open YahooFinance\n"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "// Set dotnet interactive formatter to plaintext\n",
    "Formatter.Register(fun (x:obj) (writer: TextWriter) -> fprintfn writer \"%120A\" x )\n",
    "Formatter.SetPreferredMimeTypesFor(typeof<obj>, \"text/plain\")\n",
    "// Make plotly graphs work with interactive plaintext formatter\n",
    "Formatter.SetPreferredMimeTypesFor(typeof<GenericChart.GenericChart>,\"text/html\")"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let [<Literal>] ResolutionFolder = __SOURCE_DIRECTORY__\n",
    "Environment.CurrentDirectory <- ResolutionFolder\n",
    "\n",
    "let [<Literal>] MySignalFilePath = \"data/gp_at.csv\"\n",
    "//let [<Literal>] myExcessReturnPortfoliosPath = \"data/myExcessReturnPortfolios.csv\"\n",
    "let [<Literal>] IdAndReturnsFilePath = \"data/id_and_return_data.csv\"\n",
    "\n",
    "let strategyName = \"gp_at\"\n"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "type IdAndReturnsType = \n",
    "    CsvProvider<Sample=IdAndReturnsFilePath,\n",
    "                // The schema parameter is not required,\n",
    "                // but I am using it to override some column types\n",
    "                // to make filtering easier.\n",
    "                // If I didn't do this these particular columns \n",
    "                // would have strings of \"1\" or \"0\", but explicit boolean is nicer.\n",
    "                Schema=\"obsMain(string)->obsMain=bool,exchMain(string)->exchMain=bool\",\n",
    "                ResolutionFolder=ResolutionFolder>\n",
    "\n",
    "type MySignalType = \n",
    "    CsvProvider<MySignalFilePath,\n",
    "                ResolutionFolder=ResolutionFolder>\n",
    "\n",
    "let idAndReturnsCsv = IdAndReturnsType.GetSample()\n",
    "let idAndReturnsRows = idAndReturnsCsv.Rows |> Seq.toList\n",
    "\n",
    "let mySignalCsv = MySignalType.GetSample()\n",
    "let mySignalRows = mySignalCsv.Rows |> Seq.toList"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    "## 3.1 Overview"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    " mySignalRows\n",
    "|> List.map (fun row -> row.Id)\n",
    "|> List.distinct\n",
    "|> List.length"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "// Analysis of the signal\n",
    "\n",
    "// Number of stocks per month\n",
    "let countMySignalRows (rows: list<MySignalType.Row>) = \n",
    "    let monthly =\n",
    "        mySignalRows\n",
    "        |> List.groupBy (fun row -> row.Eom)\n",
    "        |> List.sortBy (fun (month, rows) -> month)\n",
    "    [ for (month, rows) in monthly do\n",
    "        let nStocks = \n",
    "            rows\n",
    "            |> List.map (fun row -> row.Id)\n",
    "            |> List.distinct\n",
    "            |> List.length\n",
    "        month, nStocks ]\n",
    "\n",
    "let stockPerMonthCounts =\n",
    "    let ColumnChart = \n",
    "        mySignalRows\n",
    "        |> countMySignalRows\n",
    "    Chart.Column(ColumnChart, Name = \"Stocks per month\")\n",
    "        |> Chart.withXAxisStyle (TitleText=\"Month\")\n",
    "        |> Chart.withYAxisStyle (TitleText=\"Number of Stocks\")\n",
    "\n",
    "stockPerMonthCounts"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "type NonMissingSignal =\n",
    "    {\n",
    "        Id: string\n",
    "        Eom: DateTime\n",
    "        Signal: float\n",
    "    }\n",
    "\n",
    "let myNonMissingSignals =\n",
    "    mySignalRows\n",
    "    |> List.choose (fun row -> \n",
    "        match row.Signal with\n",
    "        | None -> None\n",
    "        | Some signal -> \n",
    "            Some { Id = row.Id; Eom = row.Eom; Signal = signal })\n",
    "\n",
    "let countMyNonMissingSignalRows (rows: list<NonMissingSignal>) =\n",
    "    let by_month =\n",
    "        rows\n",
    "        |> List.groupBy (fun row -> row.Eom)\n",
    "        |> List.sortBy (fun (month, rows) -> month)\n",
    "    [ for (month, rows) in by_month do\n",
    "        let nr_of_stocks = \n",
    "            rows\n",
    "            |> List.map (fun row -> row.Id)\n",
    "            |> List.distinct\n",
    "            |> List.length\n",
    "        month, nr_of_stocks ]\n",
    "\n",
    "let stock_counts_nonmissing =\n",
    "    let to_plot = \n",
    "        myNonMissingSignals\n",
    "        |> countMyNonMissingSignalRows\n",
    "    Chart.Column(to_plot, Name = \"Non Missing Stocks\")\n",
    "    |> Chart.withXAxisStyle (TitleText=\"Month\")\n",
    "    |> Chart.withYAxisStyle (TitleText=\"Number of Stocks\")\n",
    "\n",
    "stock_counts_nonmissing"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let NonMissingData =\n",
    "    myNonMissingSignals \n",
    "    |> List.map(fun x -> x.Signal)\n",
    "    |> List.toArray\n",
    "\n",
    "//Minimum\n",
    "let Minimum =\n",
    "    myNonMissingSignals\n",
    "    |> List.map(fun x -> x.Signal)\n",
    "    |> List.min\n",
    "\n",
    "// Maximum\n",
    "let Maximum =\n",
    "    myNonMissingSignals\n",
    "    |> List.map(fun x -> x.Signal)\n",
    "    |> List.max\n",
    "\n",
    "// Median\n",
    "let Median =\n",
    "    myNonMissingSignals\n",
    "    |> List.map(fun x -> x.Signal)\n",
    "    |> List.median\n",
    "\n",
    "// Standard Deviation\n",
    "let StDev = \n",
    "    myNonMissingSignals\n",
    "    |> List.map(fun x -> x.Signal)\n",
    "    |> Seq.stDev\n",
    "\n",
    "// Average\n",
    "let Average =\n",
    "    myNonMissingSignals\n",
    "    |> List.map(fun x -> x.Signal)\n",
    "    |> List.average\n",
    "\n",
    "\n",
    "printfn \"Minimum is equal to %A\" Minimum\n",
    "printfn \"Maximum is equal to %A\" Maximum\n",
    "printfn \"Median is equal to %A\" Median\n",
    "printfn \"Standard Deviation is equal to %A\" StDev\n",
    "printfn \"Average is equal to %A\" Average\n",
    "\n",
    "let signalP01: float = Quantile.compute 0.01 NonMissingData\n",
    "let signalP10: float = Quantile.compute 0.1 NonMissingData\n",
    "let signalP50: float = Quantile.compute 0.5 NonMissingData\n",
    "let signalP90: float = Quantile.compute 0.9 NonMissingData\n",
    "let signalP99: float = Quantile.compute 0.99 NonMissingData\n",
    "\n",
    "printfn \"1st percentile of the non-missing signal is equal to %A\" signalP01\n",
    "printfn \"10th percentile of the non-missing signal is equal to %A\" signalP10\n",
    "printfn \"50th percentile of the non-missing signal is equal to %A\" signalP50\n",
    "printfn \"90th percentile of the non-missing signal is equal to %A\" signalP90\n",
    "printfn \"99th percentile of the non-missing signal is equal to %A\" signalP99"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let winsorizeSignals (signalOb: NonMissingSignal) =\n",
    "    let newSignal =\n",
    "        if signalOb.Signal < signalP01 then \n",
    "            signalP01\n",
    "        elif signalOb.Signal > signalP99 then\n",
    "            signalP99\n",
    "        else\n",
    "            signalOb.Signal\n",
    "    { signalOb with Signal = newSignal }\n",
    "\n",
    "let myWinsorizedSignals =\n",
    "    myNonMissingSignals\n",
    "    |> List.map winsorizeSignals\n",
    "\n",
    "let byStockMonthSignals: list<DateTime * list<NonMissingSignal>> =\n",
    "    myWinsorizedSignals\n",
    "    |> List.groupBy(fun x -> DateTime(x.Eom.Year, x.Eom.Month, 1))\n",
    "\n",
    "byStockMonthSignals"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "\n",
    "let byStockMonthIdAndReturnMap: Map<string * DateTime, IdAndReturnsType.Row> =\n",
    "    idAndReturnsRows\n",
    "    |> List.map(fun x ->\n",
    "        let ym = x.Eom\n",
    "        let key = id x.Id, ym\n",
    "        key, x)\n",
    "    |> Map\n",
    "\n",
    "byStockMonthIdAndReturnMap.Keys\n",
    "\n",
    "let signals_winsorized_smallcap =\n",
    "    [ for i in myWinsorizedSignals do \n",
    "        match byStockMonthIdAndReturnMap |> Map.find (i.Id,i.Eom) with\n",
    "        | x when x.SizeGrp = \"small\" && x.Eom.Year = 2015 -> Some i.Signal\n",
    "        | y -> None]\n",
    "    |> List.choose id\n",
    "signals_winsorized_smallcap\n",
    "    |> Chart.Histogram"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let signals_winsorized_largecap =\n",
    "    [ for i in myWinsorizedSignals do \n",
    "        match byStockMonthIdAndReturnMap |> Map.find (i.Id,i.Eom) with\n",
    "        | x when x.SizeGrp = \"large\" && x.Eom.Year = 2015 -> Some i.Signal\n",
    "        | y -> None]\n",
    "    |> List.choose id\n",
    "signals_winsorized_largecap\n",
    "    |> Chart.Histogram"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "type SortedPort =\n",
    "    { Portfolio: int\n",
    "      Eom: DateTime\n",
    "      Stocks: list<NonMissingSignal> }\n",
    "\n",
    "let terciles =\n",
    "    byStockMonthSignals\n",
    "    |> List.collect (fun (eom, signals) ->\n",
    "        let sortedSignals =\n",
    "            signals\n",
    "            |> List.sortBy (fun signalOb -> signalOb.Signal)\n",
    "            |> List.splitInto 3\n",
    "        sortedSignals\n",
    "        |> List.mapi (fun i p -> \n",
    "            { Portfolio = i + 1\n",
    "              Eom = eom\n",
    "              Stocks = p }))\n",
    "\n",
    "let portfolio_terciles =\n",
    "    terciles\n",
    "    |> List.groupBy (fun row -> row.Portfolio)\n",
    "\n",
    "//Portfolio 1\n",
    "let portfolio_1 = \n",
    "    snd portfolio_terciles[0]\n",
    "    |> List.map (fun x -> x.Stocks)\n",
    "\n",
    "let avg_portfolio_1 = \n",
    "    [for x in portfolio_1 do\n",
    "        let m =\n",
    "            x\n",
    "            |> List.map (fun row -> row.Eom)\n",
    "            |> List.distinct\n",
    "        let avg_signal =\n",
    "            x\n",
    "            |> List.map (fun row -> row.Signal)\n",
    "            |> List.average\n",
    "        m[0], avg_signal]\n",
    "    |> List.sort\n",
    "\n",
    "//Portfolio 2\n",
    "let portfolio_2 = \n",
    "    snd portfolio_terciles[1]\n",
    "    |> List.map (fun x -> x.Stocks)\n",
    "\n",
    "let avg_portfolio_2= \n",
    "    [for x in portfolio_2 do\n",
    "        let m =\n",
    "            x\n",
    "            |> List.map (fun row -> row.Eom)\n",
    "            |> List.distinct\n",
    "        let avg_signal =\n",
    "            x\n",
    "            |> List.map (fun row -> row.Signal)\n",
    "            |> List.average\n",
    "        m[0], avg_signal]\n",
    "    |> List.sort\n",
    "\n",
    "//Portfolio 3\n",
    "let portfolio_3 = \n",
    "    snd portfolio_terciles[2]\n",
    "    |> List.map (fun x -> x.Stocks)\n",
    "\n",
    "let avg_portfolio_3 = \n",
    "    [for x in portfolio_3 do\n",
    "        let m =\n",
    "            x\n",
    "            |> List.map (fun row -> row.Eom)\n",
    "            |> List.distinct\n",
    "        let avg_signal =\n",
    "            x\n",
    "            |> List.map (fun row -> row.Signal)\n",
    "            |> List.average\n",
    "        m[0], avg_signal]\n",
    "    |> List.sort\n",
    "\n",
    "// Combined Chart\n",
    "Chart.combine (\n",
    "    [ Chart.Line(avg_portfolio_1, Name=\"1st Portfolio\")\n",
    "      Chart.Line(avg_portfolio_2, Name=\"2nd Portfolio\")\n",
    "      Chart.Line(avg_portfolio_3, Name=\"3rd Portfolio\")])"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    "## 3.2 Strategy Analysis "
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    "### Construct the Strategy"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let msfBySecurityIdAndMonth =\n",
    "    idAndReturnsRows\n",
    "    |> List.map(fun row -> \n",
    "        let id = Other row.Id\n",
    "        let month = DateTime(row.Eom.Year,row.Eom.Month,1)\n",
    "        let key = id, month\n",
    "        key, row)\n",
    "    |> Map    \n",
    "\n",
    "let signalBySecurityIdAndMonth =\n",
    "    mySignalRows\n",
    "    |> List.choose(fun row -> \n",
    "        match row.Signal with\n",
    "        | None -> None \n",
    "        | Some signal ->\n",
    "            let id = Other row.Id\n",
    "            let month = DateTime(row.Eom.Year,row.Eom.Month,1)\n",
    "            let key = id, month\n",
    "            Some (key, signal))\n",
    "    |> Map\n",
    "\n",
    "let securitiesByFormationMonth =\n",
    "    idAndReturnsRows\n",
    "    |> List.groupBy(fun x -> DateTime(x.Eom.Year, x.Eom.Month,1))\n",
    "    |> List.map(fun (ym, obsThisMonth) -> \n",
    "        let idsThisMonth = [ for x in obsThisMonth do Other x.Id ]\n",
    "        ym, idsThisMonth)\n",
    "    |> Map\n",
    "\n",
    "let getInvestmentUniverse formationMonth =\n",
    "    match Map.tryFind formationMonth securitiesByFormationMonth with\n",
    "    | Some securities -> \n",
    "        { FormationMonth = formationMonth \n",
    "          Securities = securities }\n",
    "    | None -> failwith $\"{formationMonth} is not in the date range\"\n",
    "\n",
    "\n",
    "// High signal of gp_at predicts high returns, no need to multiply for -1\n",
    "let getMySignal (securityId, formationMonth) =\n",
    "    match Map.tryFind (securityId, formationMonth) signalBySecurityIdAndMonth with\n",
    "    | None -> None\n",
    "    | Some signal ->\n",
    "        Some { SecurityId = securityId\n",
    "               Signal = signal }\n",
    "\n",
    "// For the whole investment universe\n",
    "let getMySignals (investmentUniverse: InvestmentUniverse) =\n",
    "    let listOfSecuritySignals =\n",
    "        investmentUniverse.Securities\n",
    "        |> List.choose(fun security -> \n",
    "            getMySignal (security, investmentUniverse.FormationMonth))    \n",
    "    \n",
    "    { FormationMonth = investmentUniverse.FormationMonth \n",
    "      Signals = listOfSecuritySignals }\n",
    "\n",
    "// My market capitalization\n",
    "let getMarketCap (security, formationMonth) =\n",
    "    match Map.tryFind (security, formationMonth) msfBySecurityIdAndMonth with\n",
    "    | None -> None\n",
    "    | Some row -> \n",
    "        match row.MarketEquity with\n",
    "        | None -> None\n",
    "        | Some me -> Some (security, me)\n",
    "\n",
    "let getSecurityReturn (security, formationMonth) =\n",
    "    // If the security has a missing return, assume that we got 0.0.\n",
    "    // Note: If we were doing excess returns, we would need 0.0 - rf.\n",
    "    let missingReturn = 0.0\n",
    "    match Map.tryFind (security, formationMonth) msfBySecurityIdAndMonth with\n",
    "    | None -> security, missingReturn\n",
    "    | Some x ->  \n",
    "        match x.Ret with \n",
    "        | None -> security, missingReturn\n",
    "        | Some r -> security, r\n",
    "\n",
    "// Restrictions\n",
    "let isObsMain (security, formationMonth) =\n",
    "    match Map.tryFind (security, formationMonth) msfBySecurityIdAndMonth with\n",
    "    | None -> false\n",
    "    | Some row -> row.ObsMain\n",
    "\n",
    "let isPrimarySecurity (security, formationMonth) =\n",
    "    match Map.tryFind (security, formationMonth) msfBySecurityIdAndMonth with\n",
    "    | None -> false\n",
    "    | Some row -> row.PrimarySec\n",
    "\n",
    "let isCommonStock (security, formationMonth) =\n",
    "    match Map.tryFind (security, formationMonth) msfBySecurityIdAndMonth with\n",
    "    | None -> false\n",
    "    | Some row -> row.Common\n",
    "\n",
    "let isExchMain (security, formationMonth) =\n",
    "    match Map.tryFind (security, formationMonth) msfBySecurityIdAndMonth with\n",
    "    | None -> false\n",
    "    | Some row -> row.ExchMain\n",
    "\n",
    "let hasMarketEquity (security, formationMonth) =\n",
    "    match Map.tryFind (security, formationMonth) msfBySecurityIdAndMonth with\n",
    "    | None -> false\n",
    "    | Some row -> row.MarketEquity.IsSome\n",
    "\n",
    "let myFilters securityAndFormationMonth =\n",
    "    isObsMain securityAndFormationMonth &&\n",
    "    isPrimarySecurity securityAndFormationMonth &&\n",
    "    isCommonStock securityAndFormationMonth &&\n",
    "    isExchMain securityAndFormationMonth &&\n",
    "    isExchMain securityAndFormationMonth &&\n",
    "    hasMarketEquity securityAndFormationMonth\n",
    "\n",
    "let doMyFilters (universe:InvestmentUniverse) =\n",
    "    let filtered = \n",
    "        universe.Securities\n",
    "        // my filters expect security, formationMonth\n",
    "        |> List.map(fun security -> security, universe.FormationMonth)\n",
    "        // do the filters\n",
    "        |> List.filter myFilters\n",
    "        // now convert back from security, formationMonth -> security\n",
    "        |> List.map fst\n",
    "    { universe with Securities = filtered }\n",
    "\n",
    "let formStrategy ym =\n",
    "    ym\n",
    "    |> getInvestmentUniverse\n",
    "    |> doMyFilters\n",
    "    |> getMySignals\n",
    "    |> assignSignalSort strategyName 3\n",
    "    |> List.map (giveValueWeights getMarketCap)\n",
    "    |> List.map (getPortfolioReturn getSecurityReturn)  \n",
    "\n",
    "// Define Sample months\n",
    "\n",
    "let startSample = \n",
    "    idAndReturnsRows\n",
    "    |> List.map(fun row -> DateTime(row.Eom.Year,row.Eom.Month,1))\n",
    "    |> List.min\n",
    "\n",
    "let endSample = \n",
    "    let lastMonthWithData = \n",
    "        idAndReturnsRows\n",
    "        |> Seq.map(fun row -> DateTime(row.Eom.Year,row.Eom.Month,1))\n",
    "        |> Seq.max\n",
    "    // The end of sample is the last month when we have returns.\n",
    "    // So the last month when we can form portfolios is one month\n",
    "    // before that.\n",
    "    lastMonthWithData.AddMonths(-1) \n",
    "\n",
    "let sampleMonths = getSampleMonths (startSample, endSample)\n",
    "\n",
    "// My strategy Portfolio \n",
    "\n",
    "let doParallel = true\n",
    "let portfolios =\n",
    "    if doParallel then\n",
    "        sampleMonths\n",
    "        |> List.toArray\n",
    "        |> Array.Parallel.map formStrategy\n",
    "        |> Array.toList\n",
    "        |> List.collect id\n",
    "    else\n",
    "        sampleMonths\n",
    "        |> List.collect formStrategy"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    "Form a long-short strategy portfolio that is long your top portfolio and short your bottom portfolio."
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let ff3 = French.getFF3 Frequency.Monthly\n",
    "let monthlyRiskFreeRate =\n",
    "    [ for obs in ff3 do \n",
    "        let key = DateTime(obs.Date.Year,obs.Date.Month,1)\n",
    "        key, obs.Rf ]\n",
    "    |> Map\n",
    "\n",
    "let portfolioExcessReturns =\n",
    "    portfolios\n",
    "    |> List.map(fun x -> \n",
    "        match Map.tryFind x.YearMonth monthlyRiskFreeRate with \n",
    "        | None -> failwith $\"Can't find risk-free rate for {x.YearMonth}\"\n",
    "        | Some rf -> { x with Return = x.Return - rf })\n",
    "\n",
    "let long = \n",
    "    portfolioExcessReturns \n",
    "    |> List.filter(fun x -> \n",
    "        x.PortfolioId = Indexed {| Name = strategyName ; Index = 3 |})\n",
    "\n",
    "let short = \n",
    "    portfolioExcessReturns \n",
    "    |> List.filter(fun x -> \n",
    "        x.PortfolioId = Indexed {| Name = strategyName; Index = 1 |})\n",
    "\n",
    "let longShort = \n",
    "    let shortByYearMonthMap = \n",
    "        short \n",
    "        |> List.map(fun row -> row.YearMonth, row) \n",
    "        |> Map\n",
    "    \n",
    "    [ for longObs in long do\n",
    "        match Map.tryFind longObs.YearMonth shortByYearMonthMap with\n",
    "        | None -> failwith \"probably your date variables are not aligned for a weird reason\"\n",
    "        | Some shortObs ->\n",
    "            { PortfolioId = Named \"Long-Short gp_at\"\n",
    "              YearMonth = longObs.YearMonth\n",
    "              Return = longObs.Return - shortObs.Return } ] \n",
    "\n",
    "// Plot\n",
    "let cumulateSimpleReturn (xs: PortfolioReturn list) =\n",
    "    let accumulator (priorObs:PortfolioReturn) (thisObs:PortfolioReturn) =\n",
    "        let asOfNow = (1.0 + priorObs.Return)*(1.0 + thisObs.Return) - 1.0\n",
    "        { thisObs with Return = asOfNow}\n",
    "    match xs |> List.sortBy(fun x -> x.YearMonth) with\n",
    "    | [] -> []     \n",
    "    | head::tail -> \n",
    "        (head, tail) \n",
    "        ||> List.scan accumulator\n",
    "\n",
    "let longshortCumulative = longShort |> cumulateSimpleReturn\n",
    "\n",
    "let longshortCumulativeChart =\n",
    "    longshortCumulative\n",
    "    |> List.map(fun x -> x.YearMonth, x.Return)\n",
    "    |> Chart.Line \n",
    "    |> Chart.withTitle \"Growth of 1 Euro\""
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "longshortCumulativeChart"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    "### Analyze the performance of your long-only and long-short portfolios.\n",
    "Plot cumulative returns for your two portfolios and for the excess returns of the value-weighted stock market portfolio (from the Ken\n",
    "French data, MktRf) in one chart. You should make two graphs:\n",
    "- one graph showing cumulative returns for all the portfolios.\n",
    "- one graph showing cumulative returns with a constant leverage\n",
    "applied to each portfolio so that they all have an annualized\n",
    "volatility of 10% over the full sample."
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "// Add value weighted portfolio with same time range\n",
    "\n",
    "let portfolioReturnPlot (xs:PortfolioReturn list) =\n",
    "    xs\n",
    "    |> List.map(fun x -> x.YearMonth, x.Return)\n",
    "    |> Chart.Line \n",
    "    |> Chart.withTitle \"Cumulative Returns\"\n",
    "\n",
    "let vwMktRf =\n",
    "    let portfolioMonths = \n",
    "        portfolioExcessReturns \n",
    "        |> List.map(fun x -> x.YearMonth)\n",
    "    let minYm = portfolioMonths |> List.min\n",
    "    let maxYm = portfolioMonths |> List.max\n",
    "    \n",
    "    [ for x in ff3 do\n",
    "        if x.Date >= minYm && x.Date <= maxYm then\n",
    "            { PortfolioId = Named(\"Mkt-Rf\")\n",
    "              YearMonth = x.Date\n",
    "              Return = x.MktRf } ]\n",
    "\n",
    "\n",
    "// Plot cumulative returns for your two portfolios \n",
    "// and for the excess returns of the value-weighted stock market portfolio\n",
    "\n",
    "// One graph with all the portfolio\n",
    "let combinedChart =\n",
    "    List.concat [long; longShort; vwMktRf]\n",
    "    |> List.groupBy(fun x -> x.PortfolioId)\n",
    "    |> List.map(fun (portId, xs) ->\n",
    "        xs\n",
    "        |> cumulateSimpleReturn\n",
    "        |> portfolioReturnPlot\n",
    "        |> Chart.withTraceInfo (Name=portId.ToString())\n",
    "        |> Chart.withTitle \"Cumulative Returns\"\n",
    "        |> Chart.withSize(900,600))\n",
    "    |> Chart.combine\n",
    "\n",
    "combinedChart"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "// One graph showing cumulative returns with a constant leverage applied to each \n",
    "// portfolio so that they all have an annualized volatility of 10% over the full sample.\n",
    "\n",
    "let annualizeMonthlyStdDev monthlyStdDev: float = sqrt(12.0) * monthlyStdDev\n",
    "let get_stdevAnnualized (input: PortfolioReturn list) = \n",
    "    input\n",
    "    |> Seq.stDevBy (fun x -> x.Return)\n",
    "    |> annualizeMonthlyStdDev\n",
    "\n",
    "let stdevLongAnnualized = get_stdevAnnualized long\n",
    "let stdevLongShortAnnualized = get_stdevAnnualized longShort\n",
    "let stdevVwMktRfAnnualized = get_stdevAnnualized vwMktRf\n",
    "\n",
    "\n",
    "let leverage_fun input: float =\n",
    "    0.1 / input\n",
    "let leverageLong = leverage_fun stdevLongAnnualized\n",
    "let leverageLongShort = leverage_fun stdevLongShortAnnualized\n",
    "let leverageVwMktRf = leverage_fun stdevVwMktRfAnnualized\n",
    "\n",
    "let long10_initial = \n",
    "    long\n",
    "    |> List.map(fun (x) ->\n",
    "        { PortfolioId = x.PortfolioId;\n",
    "          YearMonth = x.YearMonth;\n",
    "          Return = leverageLong * x.Return })\n",
    "\n",
    "let long10 = \n",
    "    long10_initial\n",
    "    |> List.map (fun x -> \n",
    "        { PortfolioId = Named \"Gp_at Long 10% Stdev\"\n",
    "          YearMonth = x.YearMonth\n",
    "          Return = x.Return })\n",
    "\n",
    "let longShort10_initial = \n",
    "    longShort\n",
    "    |> List.map(fun (x) ->\n",
    "        { PortfolioId = Named \"Gp_at Short 10% Stdev\";\n",
    "          YearMonth = x.YearMonth;\n",
    "          Return = leverageLongShort * x.Return })\n",
    "\n",
    "let longShort10 = \n",
    "    longShort10_initial\n",
    "    |> List.map (fun x -> \n",
    "        { PortfolioId = Named \"Gp_at Long-Short 10% StdDev\"\n",
    "          YearMonth = x.YearMonth\n",
    "          Return = x.Return })\n",
    "\n",
    "let vwMktRf10_initial = \n",
    "    vwMktRf\n",
    "    |> List.map(fun (x) ->\n",
    "        { PortfolioId = x.PortfolioId;\n",
    "          YearMonth = x.YearMonth;\n",
    "          Return = leverageVwMktRf * x.Return })\n",
    "\n",
    "let vwMktRf10 = \n",
    "    vwMktRf10_initial\n",
    "    |> List.map (fun x -> \n",
    "        { PortfolioId = Named \"Mkt-Rf 10% StdDev\"\n",
    "          YearMonth = x.YearMonth\n",
    "          Return = x.Return })\n",
    "\n",
    "let combinedChart_lev10 =\n",
    "    List.concat [long10; longShort10; vwMktRf10]\n",
    "    |> List.groupBy(fun x -> x.PortfolioId)\n",
    "    |> List.map(fun (portId, xs) ->\n",
    "        xs\n",
    "        |> cumulateSimpleReturn\n",
    "        |> portfolioReturnPlot\n",
    "        |> Chart.withTraceInfo (Name=portId.ToString())\n",
    "        |> Chart.withTitle \"Cumulative Returns levered\"\n",
    "        |> Chart.withSize(900,600))\n",
    "    |> Chart.combine\n",
    "\n",
    "combinedChart_lev10"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "source": [
    "Create a table to report performance measures for your long-only,\n",
    "long-short, and value-weighted market portfolios for the first half of the sample, \n",
    "the second half, and the full period.\n",
    "It is not necessary to report results for the 10% volatility versions in this table. \n",
    "\n",
    "For each period report:\n",
    "For the long-only and long-short portfolios:\n",
    "- What is their average annualized excess return?\n",
    "- What are their annualized Sharpe ratios?\n",
    "- What are their CAPM and Fama-French 3-factor alphas and t-statistics for these alphas?\n",
    "- What are their information ratios?\n",
    "\n",
    "For the value-weighted market portfolio:\n",
    "- What is the average annualized excess return? \n",
    "- What are the annualized Sharpe ratio?"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let first_half (lst:list<'a>) = \n",
    "    let len = List.length lst\n",
    "    let mid = int(len / 2)\n",
    "    let first_list = lst |> Seq.take mid |> Seq.toList\n",
    "    first_list\n",
    "\n",
    "let second_half (lst:list<'a>) = \n",
    "    let len = List.length lst\n",
    "    let mid = int(len / 2)\n",
    "    let second_list = lst |> Seq.skip mid |> Seq.toList\n",
    "    second_list"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "// Split the sample\n",
    "let longFirstHalf = (first_half long)\n",
    "let longSecondHalf = (second_half long)\n",
    "\n",
    "let longshortFirstHalf = (first_half longShort)\n",
    "let longshortSecondHalf = (second_half longShort)\n",
    "\n",
    "let vwMktFirstHalf = (first_half vwMktRf)\n",
    "let vwMkktSecondHalf = (second_half vwMktRf)\n",
    "\n",
    "// Average Annualized Excess Return \n",
    "let annualizeMonthlyReturns monthlyReturn = 12.0 * monthlyReturn\n",
    "\n",
    "let ann_avg (input:PortfolioReturn List) = \n",
    "    let value = \n",
    "        input\n",
    "        |> List.map (fun value -> value.Return)\n",
    "        |> List.map ( fun value -> annualizeMonthlyReturns value)\n",
    "        |> List.average\n",
    "    let y = value * 100.0\n",
    "    y\n",
    "\n",
    "let long_avg = ann_avg long\n",
    "let long_FirstHalf_avg =  ann_avg longFirstHalf\n",
    "let long_SecondHalf_avg =  ann_avg longSecondHalf\n",
    "\n",
    "let longShort_avg = ann_avg longShort\n",
    "let longShort_FirstHalf_avg = ann_avg longshortFirstHalf\n",
    "let longShort_SecondHalf_avg = ann_avg longshortSecondHalf\n",
    "\n",
    "let vwMktRf_avg = ann_avg  vwMktRf\n",
    "let vwMktRf_FirstHalf_avg = ann_avg  vwMktFirstHalf \n",
    "let vwMktRf_SecondHalf_avg = ann_avg  vwMkktSecondHalf\n",
    "\n",
    "\n",
    "// Annualized Sharpe Ratio\n",
    "\n",
    "let Sharpe (xs: float seq) =\n",
    "    (Seq.mean xs) / (Seq.stDev xs)\n",
    "\n",
    "let annualizeMonthlySharpe monthlySharpe = sqrt(12.0) * monthlySharpe\n",
    "\n",
    "let ann_sharpe (input: PortfolioReturn List) = \n",
    "    input\n",
    "    |> List.map (fun x -> x.Return)\n",
    "    |> Sharpe\n",
    "    |> annualizeMonthlySharpe\n",
    "\n",
    "let long_sharpe = ann_sharpe long\n",
    "let long_FirstHalf_sharpe =  ann_sharpe longFirstHalf\n",
    "let long_SecondHalf_sharpe =  ann_sharpe longSecondHalf\n",
    "\n",
    "let longShort_sharpe = ann_sharpe longShort\n",
    "let longShort_FirstHalf_sharpe = ann_sharpe longshortFirstHalf\n",
    "let longShort_SecondHalf_sharpe = ann_sharpe longshortSecondHalf\n",
    "\n",
    "let vwMktRf_sharpe = ann_sharpe  vwMktRf\n",
    "let vwMktRf_FirstHalf_sharpe = ann_sharpe vwMktFirstHalf \n",
    "let vwMktRf_SecondHalf_sharpe = ann_sharpe vwMkktSecondHalf\n",
    "\n"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let header = [\"\";\"Avg Annualized Excess Return\"]\n",
    "\n",
    "let rows = [\n",
    "                [\"Long\"; string long_avg];\n",
    "                [\"Long 1st half\"; string long_FirstHalf_avg];\n",
    "                [\"Long 2nd half\"; string long_SecondHalf_avg];\n",
    "                [\"Long-Short\"; string longShort_avg];\n",
    "                [\"Long-Short 1st half\"; string longShort_FirstHalf_avg];\n",
    "                [\"Long-Short 2nd half\"; string longShort_SecondHalf_avg];\n",
    "                [\"MktRf\"; string vwMktRf_avg];\n",
    "                [\"MktRf 1st half\"; string vwMktRf_FirstHalf_avg];\n",
    "                [\"MktRf 2nd half\"; string vwMktRf_SecondHalf_avg]\n",
    "]\n",
    "\n",
    "Chart.Table(header, \n",
    "            rows) \n",
    "    |> Chart.withSize (800, 400)"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let header = [\"\";\"Sharpe Ratio\"]\n",
    "\n",
    "let rows = [\n",
    "                [\"Long\"; string long_sharpe ];\n",
    "                [\"Long 1st half\"; string long_FirstHalf_sharpe ];\n",
    "                [\"Long 2nd half\"; string long_SecondHalf_sharpe];\n",
    "                [\"Long-Short\"; string longShort_sharpe];\n",
    "                [\"Long-Short 1st half\"; string longShort_FirstHalf_sharpe];\n",
    "                [\"Long-Short 2nd half\";string longShort_SecondHalf_sharpe];\n",
    "                [\"MktRf\"; string vwMktRf_sharpe;  ];\n",
    "                [\"MktRf 1st half\"; string vwMktRf_FirstHalf_sharpe];\n",
    "                [\"MktRf 2nd half\"; string vwMktRf_SecondHalf_sharpe]\n",
    "]\n",
    "\n",
    "Chart.Table(header, \n",
    "            rows) \n",
    "    |> Chart.withSize (800, 400)"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "// Alpha, tstat and Information Ratio\n",
    "type RegData =\n",
    "    { Date : DateTime\n",
    "      Portfolio : float\n",
    "      MktRf : float \n",
    "      Hml : float \n",
    "      Smb : float }\n",
    "\n",
    "let ff3ByMonth = \n",
    "    ff3\n",
    "    |> Array.map(fun x -> DateTime(x.Date.Year, x.Date.Month,1), x)\n",
    "    |> Map\n",
    "\n",
    "let reg_data (input: PortfolioReturn list) = \n",
    "    input \n",
    "    |> List.map(fun port ->\n",
    "        let month_list = DateTime(port.YearMonth.Year,port.YearMonth.Month,1)\n",
    "        match Map.tryFind month_list ff3ByMonth with\n",
    "        | None -> failwith \"probably you messed up your days of months\"\n",
    "        | Some ff3 -> \n",
    "            { Date = month_list\n",
    "              Portfolio = port.Return\n",
    "              MktRf = ff3.MktRf \n",
    "              Hml = ff3.Hml \n",
    "              Smb = ff3.Smb })\n",
    "    |> List.toArray\n",
    "\n",
    "let capmModelData (input: RegData array) = \n",
    "    input\n",
    "    |> Array.map(fun obs -> [|obs.MktRf|], obs.Portfolio)\n",
    "    |> Array.unzip \n",
    "\n",
    "let ff3ModelData (input: RegData array) =\n",
    "    input\n",
    "    |> Array.map(fun obs -> [|obs.MktRf; obs.Hml; obs.Smb |], obs.Portfolio)\n",
    "    |> Array.unzip\n",
    "\n",
    "type RegressionOutput =\n",
    "    { Model : MultipleLinearRegression \n",
    "      TValuesWeights : float array\n",
    "      TValuesIntercept : float \n",
    "      R2: float }\n",
    "\n",
    "type XY = (float array) array * float array\n",
    "\n",
    "let fitModel (x: (float array) array, y: float array) =\n",
    "    let ols = new OrdinaryLeastSquares(UseIntercept=true)\n",
    "    let estimate = ols.Learn(x,y)\n",
    "    let mse = estimate.GetStandardError(x,y)\n",
    "    let se = estimate.GetStandardErrors(mse, ols.GetInformationMatrix())\n",
    "    let tvaluesWeights = \n",
    "        estimate.Weights\n",
    "        |> Array.mapi(fun i w -> w / se.[i])\n",
    "    let tvalueIntercept = estimate.Intercept / (se |> Array.last)\n",
    "    let r2 = estimate.CoefficientOfDetermination(x,y)\n",
    "    { Model = estimate\n",
    "      TValuesWeights = tvaluesWeights\n",
    "      TValuesIntercept = tvalueIntercept  \n",
    "      R2 = r2 }\n",
    "\n",
    "let capmEstimate (input: PortfolioReturn list) =\n",
    "    reg_data input\n",
    "    |> capmModelData\n",
    "    |> fitModel\n",
    "\n",
    "let ff3Estimate (input: PortfolioReturn list) = \n",
    "    reg_data input\n",
    "    |> ff3ModelData\n",
    "    |> fitModel\n",
    "\n",
    "// Information Ratios\n",
    "\n",
    "type Prediction = { Label : float; Score : float}\n",
    "\n",
    "let makePredictions \n",
    "    (estimate:MultipleLinearRegression) \n",
    "    (x: (float array) array, y: float array) =\n",
    "    (estimate.Transform(x), y)\n",
    "    ||> Array.zip\n",
    "    |> Array.map(fun (score, label) -> { Score = score; Label = label })\n",
    "\n",
    "let residuals (xs: Prediction array) = xs |> Array.map(fun x -> x.Label - x.Score)\n",
    "\n",
    "let informationRatio monthlyAlpha (monthlyResiduals: float array) =\n",
    "    let annualAlpha = 12.0 * monthlyAlpha\n",
    "    let annualStDev = sqrt(12.0) * (Seq.stDev monthlyResiduals)\n",
    "    annualAlpha / annualStDev\n",
    "\n",
    "let capmModelData_reg (input: PortfolioReturn list) =\n",
    "    reg_data input\n",
    "    |> capmModelData\n",
    "\n",
    "let ff3ModelData_reg (input: PortfolioReturn list) =\n",
    "    reg_data input\n",
    "    |> ff3ModelData\n",
    "\n",
    "let capmInformationRatio (input: PortfolioReturn list)  =\n",
    "    let x =\n",
    "        makePredictions (capmEstimate input).Model (capmModelData_reg input)\n",
    "        |> residuals\n",
    "    informationRatio (capmEstimate input).Model.Intercept x\n",
    "\n",
    "let ff3InformationRatio (input: PortfolioReturn list)  =\n",
    "    let x =\n",
    "        makePredictions (ff3Estimate input).Model (ff3ModelData_reg input)\n",
    "        |> residuals\n",
    "    informationRatio (ff3Estimate input).Model.Intercept x"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "// For the long portfolio\n",
    "\n",
    "// CAPM\n",
    "let capmEstimate_long1 = capmEstimate longFirstHalf\n",
    "let capmEstimate_long2 = capmEstimate longSecondHalf\n",
    "let capmEstimate_long = capmEstimate long\n",
    "\n",
    "let capmAlpha_long1 = 12.0 * capmEstimate_long1.Model.Intercept\n",
    "let capmAlpha_long2 = 12.0 * capmEstimate_long2.Model.Intercept \n",
    "let capmAlpha_long = 12.0 * capmEstimate_long.Model.Intercept \n",
    "\n",
    "let capmTstat_long1 = 12.0 * capmEstimate_long1.TValuesIntercept\n",
    "let capmTstat_long2 = 12.0 * capmEstimate_long2.TValuesIntercept\n",
    "let capmTstat_long = 12.0 * capmEstimate_long.TValuesIntercept\n",
    "\n",
    "let capmInformationratio_long1 = capmInformationRatio longFirstHalf\n",
    "let capmInformationratio_long2 = capmInformationRatio longSecondHalf\n",
    "let capmInformationratio_long = capmInformationRatio long\n",
    "\n",
    "// FF3\n",
    "let ff3Estimate_long1 = ff3Estimate longFirstHalf\n",
    "let ff3Estimate_long2 = ff3Estimate longSecondHalf\n",
    "let ff3Estimate_long = ff3Estimate long\n",
    "\n",
    "let ff3Alpha_long1 = 12.0 * ff3Estimate_long1.Model.Intercept \n",
    "let ff3Alpha_long2 = 12.0 * ff3Estimate_long2.Model.Intercept \n",
    "let ff3Alpha_long = 12.0 * ff3Estimate_long.Model.Intercept\n",
    "\n",
    "let ff3Tstat_long1 = 12.0 * ff3Estimate_long1.TValuesIntercept\n",
    "let ff3Tstat_long2 = 12.0 * ff3Estimate_long2.TValuesIntercept \n",
    "let ff3Tstat_long = 12.0 * ff3Estimate_long.TValuesIntercept\n",
    "\n",
    "let ff3Informationratio_long1 = ff3InformationRatio longFirstHalf\n",
    "let ff3Informationratio_long2 = ff3InformationRatio longSecondHalf\n",
    "let ff3Informationratio_long = ff3InformationRatio long\n"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "// Alphas and tstat for long-short portfolio\n",
    "\n",
    "// CAPM\n",
    "let capmEstimate_longshort1 = capmEstimate longshortFirstHalf\n",
    "let capmEstimate_longshort2 = capmEstimate longshortSecondHalf\n",
    "let capmEstimate_longshort = capmEstimate longShort\n",
    "\n",
    "let capmAlpha_longshort1 = 12.0 * capmEstimate_longshort1.Model.Intercept\n",
    "let capmAlpha_longshort2 = 12.0 * capmEstimate_longshort2.Model.Intercept \n",
    "let capmAlpha_longshort = 12.0 * capmEstimate_longshort.Model.Intercept \n",
    "\n",
    "let capmTstat_longshort1 = 12.0 * capmEstimate_longshort1.TValuesIntercept\n",
    "let capmTstat_longshort2 = 12.0 * capmEstimate_longshort2.TValuesIntercept\n",
    "let capmTstat_longshort = 12.0 * capmEstimate_longshort.TValuesIntercept\n",
    "\n",
    "let capmInformationratio_longshort1 = capmInformationRatio longshortFirstHalf\n",
    "let capmInformationratio_longshort2 = capmInformationRatio longshortSecondHalf\n",
    "let capmInformationratio_longshort = capmInformationRatio longShort\n",
    "\n",
    "// FF3\n",
    "let ff3Estimate_longshort1 = ff3Estimate longshortFirstHalf\n",
    "let ff3Estimate_longshort2 = ff3Estimate longshortSecondHalf\n",
    "let ff3Estimate_longshort = ff3Estimate longShort\n",
    "\n",
    "let ff3Alpha_longshort1 = 12.0 * ff3Estimate_longshort1.Model.Intercept \n",
    "let ff3Alpha_longshort2 = 12.0 * ff3Estimate_longshort2.Model.Intercept \n",
    "let ff3Alpha_longshort = 12.0 * ff3Estimate_longshort.Model.Intercept\n",
    "\n",
    "let ff3Tstat_longshort1 = 12.0 * ff3Estimate_longshort1.TValuesIntercept\n",
    "let ff3Tstat_longshort2 = 12.0 * ff3Estimate_longshort2.TValuesIntercept \n",
    "let ff3Tstat_longshort = 12.0 * ff3Estimate_longshort.TValuesIntercept\n",
    "\n",
    "let ff3Informationratio_longshort1 = ff3InformationRatio longshortFirstHalf\n",
    "let ff3Informationratio_longshort2 = ff3InformationRatio longshortSecondHalf\n",
    "let ff3Informationratio_longshort = ff3InformationRatio longShort"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let header = [\"\"; \"CAPM Alpha\"; \"CAPM t-stat\"; \"CAPM IR\"]\n",
    "\n",
    "let rows = [\n",
    "                [\"Long\"; string capmAlpha_long; string capmTstat_long; string capmInformationratio_long];\n",
    "                [\"Long 1st half\"; string capmAlpha_long1; string capmTstat_long1; string capmInformationratio_long1 ];\n",
    "                [\"Long 2nd half\"; string capmAlpha_long2; string capmTstat_long2; string capmInformationratio_long2 ];\n",
    "\n",
    "                [\"Long-Short\"; string capmAlpha_longshort; string capmTstat_longshort; string capmInformationratio_longshort ];\n",
    "                [\"Long-Short 1st half\"; string capmAlpha_longshort1; string capmTstat_longshort1; string capmInformationratio_longshort1];\n",
    "                [\"Long-Short 2nd half\"; string capmAlpha_longshort2; string capmTstat_longshort2; string capmInformationratio_longshort2]\n",
    "]\n",
    "\n",
    "Chart.Table(header, \n",
    "            rows) \n",
    "    |> Chart.withSize (1000, 400)"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let header = [\"\"; \"FF3 Alpha\"; \"FF3 t-stat\"; \"FF3 IR\"]\n",
    "\n",
    "let rows = [\n",
    "                [\"Long\"; string ff3Alpha_long; string ff3Tstat_long; string ff3Informationratio_long ];\n",
    "                [\"Long 1st half\"; string ff3Alpha_long1; string ff3Tstat_long1; string ff3Informationratio_long1];\n",
    "                [\"Long 2nd half\"; string ff3Alpha_long2; string ff3Tstat_long2; string ff3Informationratio_long2];\n",
    "\n",
    "                [\"Long-Short\"; string ff3Alpha_longshort; string ff3Tstat_longshort; string ff3Informationratio_longshort ];\n",
    "                [\"Long-Short 1st half\"; string ff3Alpha_longshort1; string ff3Tstat_longshort1; string ff3Informationratio_longshort1];\n",
    "                [\"Long-Short 2nd half\"; string ff3Alpha_longshort2; string ff3Tstat_longshort2; string ff3Informationratio_longshort2]\n",
    "                ]\n",
    "\n",
    "Chart.Table(header,\n",
    "            rows) \n",
    "    |> Chart.withSize (1000, 400)\n"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "source": [
    "## 3.3 Strategy as part of a diversified portfolio"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    "### Identify the tangency portfolio"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "type StockData =\n",
    "    { Symbol : string \n",
    "      Date : DateTime\n",
    "      Return : float }\n",
    "\n",
    "// Gett ff3 asset pricing model data and transform to a StockData record type\n",
    "let ff3new = French.getFF3 Frequency.Monthly |> Array.toList\n",
    "\n",
    "let ff3StockData =\n",
    "    [ \n",
    "       ff3new |> List.map(fun x -> {Symbol=\"HML\";Date=x.Date;Return=x.Hml})\n",
    "       ff3new |> List.map(fun x -> {Symbol=\"MktRf\";Date=x.Date;Return=x.MktRf})\n",
    "       ff3new |> List.map(fun x -> {Symbol=\"Smb\";Date=x.Date;Return=x.Smb})\n",
    "    ] |> List.concat\n",
    "\n",
    "// Factor data\n",
    "let tickers = \n",
    "    [ \n",
    "        \"VTI\" // Vanguard Total Stock Market ETF\n",
    "        \"BND\" // Vanguard Total Bond Market ETF\n",
    "    ]\n",
    "\n",
    "let tickPrices = \n",
    "    YahooFinance.PriceHistory(\n",
    "        tickers,\n",
    "        startDate = DateTime(2000,1,1),\n",
    "        interval = Monthly)\n",
    "\n",
    "tickPrices"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "// Returns from price Observations\n",
    "let pricesToReturns (symbol, adjPrices: list<PriceObs>) =\n",
    "    adjPrices\n",
    "    |> List.sortBy (fun x -> x.Date)\n",
    "    |> List.pairwise\n",
    "    |> List.map (fun (day0, day1) ->\n",
    "        let r = day1.AdjustedClose / day0.AdjustedClose - 1.0 \n",
    "        { Symbol = symbol \n",
    "          Date = day1.Date \n",
    "          Return = r })\n",
    "\n",
    "let tickReturns =\n",
    "    tickPrices\n",
    "    |> List.groupBy (fun x -> x.Symbol)\n",
    "    |> List.collect pricesToReturns"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    "Convert to excess returns"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let rf = Map [ for x in ff3 do x.Date, x.Rf ]\n",
    "let initial_date = tickReturns[0].Date\n",
    "\n",
    "let standardInvestmentsExcess_fun (input: StockData list) =\n",
    "    let fin_value = \n",
    "        let append_1 =\n",
    "            let maxff3Date = ff3new |> List.map(fun x -> x.Date) |> List.max\n",
    "            tickReturns\n",
    "            |> List.filter(fun x -> x.Date <= maxff3Date)\n",
    "            |> List.map(fun x -> \n",
    "                match Map.tryFind x.Date rf with \n",
    "                | None -> failwith $\"why isn't there a rf for {x.Date}\"\n",
    "                | Some rf -> { x with Return = x.Return - rf })\n",
    "        let append_2 =\n",
    "            let maxff3Date = ff3new |> List.map(fun x -> x.Date) |> List.max\n",
    "            input \n",
    "            |> List.filter(fun x -> x.Date >= initial_date)\n",
    "            |> List.filter(fun x -> x.Date <= maxff3Date)\n",
    "            |> List.map(fun x -> \n",
    "                match Map.tryFind x.Date rf with \n",
    "                | None -> failwith $\"why isn't there a rf for {x.Date}\"\n",
    "                | Some rf -> { x with Return = x.Return })\n",
    "        append_1 @ append_2\n",
    "        |> Seq.distinct\n",
    "        |> List.ofSeq\n",
    "    fin_value\n",
    "\n",
    "let long_new = \n",
    "    long\n",
    "    |> List.map(fun i ->\n",
    "        { Symbol = \"Long\"\n",
    "          Date = i.YearMonth\n",
    "          Return = i.Return })  \n",
    "\n",
    "let longshort_new = \n",
    "    longShort\n",
    "    |> List.map(fun i ->\n",
    "        { Symbol = \"Long-Short\"\n",
    "          Date = i.YearMonth\n",
    "          Return = i.Return })  "
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let standardInvestmentsExcessLong = standardInvestmentsExcess_fun long_new\n",
    "let standardInvestmentsExcessLongShort = standardInvestmentsExcess_fun longshort_new"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "// Put stocks in a map keyed by symbol\n",
    "let stockData_fun (input: StockData list) = \n",
    "    input\n",
    "    |> List.groupBy(fun x -> x.Symbol)\n",
    "    |> Map"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let stockDataLong = stockData_fun standardInvestmentsExcessLong\n",
    "let stockDataLongShort = stockData_fun standardInvestmentsExcessLongShort"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    "- One version using your long-only portfolio + other asset\n",
    "- Another version using your long-short portfolio + other assets."
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let tickers_long =\n",
    "    [ \n",
    "        \"VTI\" // Vanguard Total Stock Market ETF\n",
    "        \"BND\" // Vanguard Total Bond Market ETF\n",
    "        \"Long\"\n",
    "    ]\n",
    "\n",
    "let tickers_LongShort =\n",
    "    [ \n",
    "        \"VTI\" // Vanguard Total Stock Market ETF\n",
    "        \"BND\" // Vanguard Total Bond Market ETF\n",
    "        \"Long-Short\"\n",
    "    ]\n",
    "\n",
    "let tickers_6040 =\n",
    "    [ \n",
    "        \"VTI\" // Vanguard Total Stock Market ETF\n",
    "        \"BND\" // Vanguard Total Bond Market ETF\n",
    "    ]"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    " When estimating covariances/correlations, use the mutually overlapping time period"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let getCov xId yId (stockData: Map<string,StockData list>) =\n",
    "    let xRet = \n",
    "        stockData[xId] \n",
    "        |> List.map (fun x -> x.Date,x.Return) \n",
    "        |> Map\n",
    "    let yRet = \n",
    "        stockData[yId]\n",
    "        |> List.map (fun y -> y.Date, y.Return)\n",
    "        |> Map\n",
    "    let overlappingDates =\n",
    "        [ xRet.Keys |> set\n",
    "          yRet.Keys |> set]\n",
    "        |> Set.intersectMany\n",
    "    [ for date in overlappingDates do xRet[date], yRet[date]]\n",
    "    |> Seq.covOfPairs\n"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let covariances_long =\n",
    "    [ for rowTick in tickers_long do \n",
    "        [ for colTick in tickers_long do\n",
    "            getCov rowTick colTick stockDataLong ]]\n",
    "    |> dsharp.tensor\n",
    "\n",
    "covariances_long"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let covariances_longShort =\n",
    "    [ for rowTick in tickers_LongShort do \n",
    "        [ for colTick in tickers_LongShort do\n",
    "            getCov rowTick colTick stockDataLongShort ]]\n",
    "    |> dsharp.tensor\n",
    "\n",
    "covariances_longShort"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let means_long =\n",
    "    [ for ticker in tickers_long do \n",
    "        stockDataLong[ticker]\n",
    "        |> List.averageBy (fun x -> x.Return)]\n",
    "    |> dsharp.tensor\n",
    "\n",
    "let l_w' = dsharp.solve(covariances_long,means_long)\n",
    "let wLong = l_w' / l_w'.sum()\n",
    "\n",
    "let weights_long =\n",
    "    Seq.zip tickers_long (wLong.toArray1D<float>())\n",
    "    |> Map.ofSeq\n"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let means_longShort =\n",
    "    [ for ticker in tickers_LongShort do \n",
    "        stockDataLongShort[ticker]\n",
    "        |> List.averageBy (fun x -> x.Return)]\n",
    "    |> dsharp.tensor\n",
    "\n",
    "let ls_w' = dsharp.solve(covariances_longShort,means_longShort)\n",
    "let wLongShort = ls_w' / ls_w'.sum()\n",
    "\n",
    "let weights_longShort =\n",
    "    Seq.zip tickers_LongShort (wLongShort.toArray1D<float>())\n",
    "    |> Map.ofSeq"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    "### Form comparison diversified portfolios. \n",
    "You must form a 60/40 portfolio that is invested 60% in the Vanguard Total Stock Market ETF (VTI) and 40% in the Vanguard Total Bond Market ETF (BND). Make sure you use excess returns. You may add other comparison portfolios if you wish, but this is not required."
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let stockDataByDate_fun (input: seq<list<StockData>>) = \n",
    "    input\n",
    "    |> Seq.toList\n",
    "    |> List.collect id \n",
    "    |> List.groupBy(fun x -> x.Date) \n",
    "    |> List.sortBy fst "
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let stockDataByDateLong = stockDataByDate_fun stockDataLong.Values\n",
    "let stockDataByDateLongShort = stockDataByDate_fun stockDataLongShort.Values"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let allAssetsStart_long =\n",
    "    stockDataByDateLong\n",
    "    |> List.find(fun (month, stocks) -> stocks.Length = tickers_long.Length)\n",
    "    |> fst \n",
    "\n",
    "let allAssetsEnd_long =\n",
    "    stockDataByDateLong\n",
    "    |> List.findBack(fun (month, stocks) -> stocks.Length = tickers_long.Length)\n",
    "    |> fst\n",
    "\n",
    "let allAssetsStart_longShort =\n",
    "    stockDataByDateLongShort\n",
    "    |> List.find(fun (month, stocks) -> stocks.Length = tickers_LongShort.Length)\n",
    "    |> fst \n",
    "\n",
    "let allAssetsEnd_longShort =\n",
    "    stockDataByDateLongShort\n",
    "    |> List.findBack(fun (month, stocks) -> stocks.Length = tickers_LongShort.Length)\n",
    "    |> fst\n",
    "\n",
    "let stockDataByDateComplete_long =\n",
    "    stockDataByDateLong\n",
    "    |> List.filter(fun (date, stocks) -> \n",
    "        date >= allAssetsStart_long &&\n",
    "        date <= allAssetsEnd_long)\n",
    "\n",
    "let stockDataByDateComplete_longShort =\n",
    "    stockDataByDateLongShort\n",
    "    |> List.filter(fun (date, stocks) -> \n",
    "        date >= allAssetsStart_longShort &&\n",
    "        date <= allAssetsEnd_longShort)"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let checkOfCompleteDataLong =\n",
    "    stockDataByDateComplete_long\n",
    "    |> List.map snd\n",
    "    |> List.filter(fun x -> x.Length <> tickers_long.Length) // discard rows where we have all symbols.\n",
    "\n",
    "if not (List.isEmpty checkOfCompleteDataLong) then \n",
    "        failwith \"stockDataByDateComplete has months with missing stocks\"\n",
    "\n",
    "\n",
    "let checkOfCompleteData_longShort =\n",
    "    stockDataByDateComplete_longShort\n",
    "    |> List.map snd\n",
    "    |> List.filter(fun x -> x.Length <> tickers_LongShort.Length) // discard rows where we have all symbols.\n",
    "\n",
    "if not (List.isEmpty checkOfCompleteData_longShort) then \n",
    "        failwith \"stockDataByDateComplete has months with missing stocks\""
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "checkOfCompleteDataLong"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "checkOfCompleteData_longShort"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let portfolioMonthReturn (weights: Map<string,float>) (monthData: list<StockData>) =\n",
    "    weights\n",
    "    |> Map.toList\n",
    "    |> List.map(fun (symbol, weight) ->\n",
    "        let symbolData = \n",
    "            match monthData |> List.tryFind(fun x -> x.Symbol = symbol) with\n",
    "            | None -> failwith $\"You tried to find {symbol} in the data but it was not there\"\n",
    "            | Some data -> data\n",
    "        symbolData.Return*weight)\n",
    "    |> List.sum"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let weights_6040 = Map [(\"VTI\",0.6);(\"BND\",0.4)]"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let portMVE_long =\n",
    "    stockDataByDateComplete_long\n",
    "    |> List.map(fun (date, data) -> \n",
    "        { Symbol = \"MVE Long Portfolio\"\n",
    "          Date = date\n",
    "          Return = portfolioMonthReturn weights_long data })\n",
    "\n",
    "let portMVE_longShort =\n",
    "    stockDataByDateComplete_longShort\n",
    "    |> List.map(fun (date, data) -> \n",
    "        { Symbol = \"MVE Long-Short\"\n",
    "          Date = date\n",
    "          Return = portfolioMonthReturn weights_longShort data })\n",
    "\n",
    "// stockDataByDateComplete_long is used for 60-40 Portfolio\n",
    "let port_6040 = \n",
    "    stockDataByDateComplete_long\n",
    "    |> List.map(fun (date, data) -> \n",
    "        { Symbol = \"60/40 Portfolio\"\n",
    "          Date = date \n",
    "          Return = portfolioMonthReturn weights_6040 data})"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "source": [
    "### Analyze the performance of your mean-variance efficient portfolios and your comparison diversified portfolio or portfolios.\n",
    "Plot cumulative returns. You should make two graphs:\n",
    "- one graph showing cumulative returns for the portfolios.\n",
    "- one graph showing cumulative returns with a constant leverage\n",
    "applied to each portfolio so that they all have an annualized\n",
    "volatility of 10% over the full sample."
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let cumulateReturns (xs: list<StockData>) =\n",
    "    let folder (prev: StockData) (current: StockData) =\n",
    "        let newReturn = prev.Return * (1.0+current.Return)\n",
    "        { current with Return = newReturn}\n",
    "    \n",
    "    match xs |> List.sortBy (fun x -> x.Date) with\n",
    "    | [] -> []\n",
    "    | h::t ->\n",
    "        ({ h with Return = 1.0+h.Return}, t) \n",
    "        ||> List.scan folder\n",
    "\n",
    "let portMveLongCumulative = \n",
    "    portMVE_long\n",
    "    |> cumulateReturns\n",
    "\n",
    "let portMveLongShortCumulative = \n",
    "    portMVE_longShort\n",
    "    |> cumulateReturns\n",
    "\n",
    "let port6040Cumulative = \n",
    "    port_6040\n",
    "    |> cumulateReturns\n",
    "\n",
    "\n",
    "let chartMVELong = \n",
    "    portMveLongCumulative\n",
    "    |> List.map(fun x -> x.Date, x.Return)\n",
    "    |> Chart.Line\n",
    "    |> Chart.withTraceInfo(Name=\"MVE Long\")\n",
    "\n",
    "let chartMVELongShort = \n",
    "    portMveLongShortCumulative \n",
    "    |> List.map(fun x -> x.Date, x.Return)\n",
    "    |> Chart.Line\n",
    "    |> Chart.withTraceInfo(Name=\"MVE Long Short\")\n",
    "\n",
    "let chart6040 = \n",
    "    port6040Cumulative\n",
    "    |> List.map(fun x -> x.Date, x.Return)\n",
    "    |> Chart.Line\n",
    "    |> Chart.withTraceInfo(Name=\"60/40\")\n",
    "\n",
    "let chartCombined =\n",
    "    [ chartMVELong; chart6040;chartMVELongShort]\n",
    "    |> Chart.combine\n",
    "    |> Chart.withSize(900,600)\n",
    "    |> Chart.withTitle(\"Diversified Portfolio - Cumulative Returns\")\n",
    "\n",
    "chartCombined"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "portMVE_long\n",
    "|> List.map(fun x -> x.Return)\n",
    "|> Seq.stDev\n",
    "|> fun vol -> sqrt(12.0) * vol"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "portMVE_longShort\n",
    "|> List.map(fun x -> x.Return)\n",
    "|> Seq.stDev\n",
    "|> fun vol -> sqrt(12.0)*vol\n"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "port_6040\n",
    "|> List.map(fun x -> x.Return)\n",
    "|> Seq.stDev\n",
    "|> fun vol -> sqrt(12.0)*vol"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let normalize10pctVol xs =\n",
    "    let vol = xs |> List.map(fun x -> x.Return) |> Seq.stDev\n",
    "    let annualizedVol = vol * sqrt(12.0)\n",
    "    xs \n",
    "    |> List.map(fun x -> { x with Return = x.Return * (0.1/annualizedVol)})\n",
    "\n",
    "let portMveCumulativeNormlizedVol_long = \n",
    "    portMVE_long\n",
    "    |> normalize10pctVol\n",
    "    |> cumulateReturns\n",
    "\n",
    "let portMveCumulativeNormlizedVol_longshort = \n",
    "    portMVE_longShort\n",
    "    |> normalize10pctVol\n",
    "    |> cumulateReturns\n",
    "\n",
    "let port6040CumulativeNormlizedVol = \n",
    "    port_6040\n",
    "    |> normalize10pctVol \n",
    "    |> cumulateReturns\n",
    "\n",
    "\n",
    "let chartMVENormlizedVol_long = \n",
    "    portMveCumulativeNormlizedVol_long\n",
    "    |> List.map(fun x -> x.Date, x.Return)\n",
    "    |> Chart.Line\n",
    "    |> Chart.withTraceInfo(Name=\"MVE Long\")\n",
    "\n",
    "let chartMVENormlizedVol_longShort = \n",
    "    portMveCumulativeNormlizedVol_longshort\n",
    "    |> List.map(fun x -> x.Date, x.Return)\n",
    "    |> Chart.Line\n",
    "    |> Chart.withTraceInfo(Name=\"MVE Long Short\")\n",
    "\n",
    "let chart6040NormlizedVol = \n",
    "    port6040CumulativeNormlizedVol\n",
    "    |> List.map(fun x -> x.Date, x.Return)\n",
    "    |> Chart.Line\n",
    "    |> Chart.withTraceInfo(Name=\"60/40\")\n",
    "\n",
    "let chartCombinedNormlizedVol =\n",
    "    [ chartMVENormlizedVol_long; chartMVENormlizedVol_longShort; chart6040NormlizedVol]\n",
    "    |> Chart.combine\n",
    "    |> Chart.withSize(900,600)\n",
    "    |> Chart.withTitle(\"Diversified Portfolio - Cumulative Returns Levered\")\n",
    "\n",
    "chartCombinedNormlizedVol"
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "source": [
    "Create a table to report performance measures for the portfolios over\n",
    "the full period:\n",
    "- What is their average annualized return? \n",
    "- What are their annualized Sharpe ratios?\n"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let avg_StockDataLong  = \n",
    "    let x = \n",
    "        portMVE_long\n",
    "        |> List.map (fun x -> x.Return)\n",
    "        |> List.map ( fun x -> annualizeMonthlyReturns x)\n",
    "        |> List.average\n",
    "    let y = \n",
    "        x * 100.0\n",
    "    y\n",
    "\n",
    "let avg_StockDataLongShort  = \n",
    "    let x = \n",
    "        portMVE_longShort\n",
    "        |> List.map (fun x -> x.Return)\n",
    "        |> List.map ( fun x -> annualizeMonthlyReturns x)\n",
    "        |> List.average\n",
    "    let y = \n",
    "        x * 100.0\n",
    "    y\n",
    "\n",
    "let avg_StockData_6040  = \n",
    "    let x = \n",
    "        port_6040\n",
    "        |> List.map (fun x -> x.Return)\n",
    "        |> List.map ( fun x -> annualizeMonthlyReturns x)\n",
    "        |> List.average\n",
    "    let y = \n",
    "        x * 100.0\n",
    "    y"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let sharpe_StockData_Long = \n",
    "    portMVE_long\n",
    "    |> List.map (fun x -> x.Return)\n",
    "    |> Sharpe\n",
    "    |> annualizeMonthlySharpe\n",
    "\n",
    "let sharpe_StockData_LongShort = \n",
    "    portMVE_longShort\n",
    "    |> List.map (fun x -> x.Return)\n",
    "    |> Sharpe\n",
    "    |> annualizeMonthlySharpe\n",
    "\n",
    "let sharpe_StockData_6040 = \n",
    "    port_6040\n",
    "    |> List.map (fun x -> x.Return)\n",
    "    |> Sharpe\n",
    "    |> annualizeMonthlySharpe"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let header = [\"\";\"Avg Annualized Returns\"; \"Annualized Sharpe Ratio\"]\n",
    "\n",
    "let rows = [\n",
    "                [\"Long\"; string avg_StockDataLong; string sharpe_StockData_Long ];\n",
    "                [\"Long-Short\"; string avg_StockDataLongShort ; string sharpe_StockData_LongShort];\n",
    "                [\"60-40\"; string avg_StockData_6040;string sharpe_StockData_6040 ];\n",
    "]\n",
    "\n",
    "Chart.Table(header, \n",
    "            rows) \n",
    "    |> Chart.withSize (800, 400)"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let portMveNormlizedVol_long = \n",
    "    portMVE_long\n",
    "    |> normalize10pctVol \n",
    "\n",
    "let portMveNormlizedVol_longshort = \n",
    "    portMVE_longShort\n",
    "    |> normalize10pctVol  \n",
    "\n",
    "let port6040NormlizedVol = \n",
    "    port_6040\n",
    "    |> normalize10pctVol \n",
    "\n",
    "let avg_StockDataLong_10  = \n",
    "    let x = \n",
    "        portMveNormlizedVol_long\n",
    "        |> List.map (fun x -> x.Return)\n",
    "        |> List.map ( fun x -> annualizeMonthlyReturns x)\n",
    "        |> List.average\n",
    "    let y = \n",
    "        x * 100.0\n",
    "    y\n",
    "\n",
    "let avg_StockDataLongShort_10  = \n",
    "    let x = \n",
    "        portMveNormlizedVol_longshort\n",
    "        |> List.map (fun x -> x.Return)\n",
    "        |> List.map ( fun x -> annualizeMonthlyReturns x)\n",
    "        |> List.average\n",
    "    let y = \n",
    "        x * 100.0\n",
    "    y\n",
    "\n",
    "let avg_StockData_6040_10  = \n",
    "    let x = \n",
    "        port6040NormlizedVol\n",
    "        |> List.map (fun x -> x.Return)\n",
    "        |> List.map ( fun x -> annualizeMonthlyReturns x)\n",
    "        |> List.average\n",
    "    let y = \n",
    "        x * 100.0\n",
    "    y\n",
    "\n",
    "let sharpe_StockData_Long_10 = \n",
    "    portMveNormlizedVol_long\n",
    "    |> List.map (fun x -> x.Return)\n",
    "    |> Sharpe\n",
    "    |> annualizeMonthlySharpe\n",
    "\n",
    "let sharpe_StockData_LongShort_10 = \n",
    "    portMveNormlizedVol_longshort\n",
    "    |> List.map (fun x -> x.Return)\n",
    "    |> Sharpe\n",
    "    |> annualizeMonthlySharpe\n",
    "\n",
    "let sharpe_StockData_6040_10 = \n",
    "    port6040NormlizedVol\n",
    "    |> List.map (fun x -> x.Return)\n",
    "    |> Sharpe\n",
    "    |> annualizeMonthlySharpe"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "dotnet_interactive": {
     "language": "fsharp"
    }
   },
   "outputs": [],
   "source": [
    "let header = [\"\";\"Avg Annualized Returns 10%\"; \"Annualized Sharpe Ratio 10%\"]\n",
    "\n",
    "let rows = [\n",
    "                [\"Long\"; string avg_StockDataLong_10; string sharpe_StockData_Long_10 ];\n",
    "                [\"Long-Short\"; string avg_StockDataLongShort_10 ; string sharpe_StockData_LongShort_10];\n",
    "                [\"60-40\"; string avg_StockData_6040_10;string sharpe_StockData_6040_10 ];\n",
    "]\n",
    "\n",
    "Chart.Table(header, \n",
    "            rows) \n",
    "    |> Chart.withSize (800, 400)"
   ]
  }
 ],
 "metadata": {
  "kernelspec": {
   "display_name": ".NET (C#)",
   "language": "C#",
   "name": ".net-csharp"
  },
  "language_info": {
   "file_extension": ".cs",
   "mimetype": "text/x-csharp",
   "name": "C#",
   "pygments_lexer": "csharp",
   "version": "9.0"
  }
 },
 "nbformat": 4,
 "nbformat_minor": 2
}
