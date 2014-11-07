﻿using System;
using System.Diagnostics;
using System.Linq;
using org.pescuma.progressmonitor.utils;

namespace org.pescuma.progressmonitor.devel
{
	public class ComputeRelativeTimesProgressMonitor : ProgressMonitor
	{
		private readonly ProgressMonitor next;
		private readonly Action<string> writeLine;
		private Step[] steps;
		private int currentStep = -1;
		private DateTime start;

		public ComputeRelativeTimesProgressMonitor(ProgressMonitor next, Action<string> writeLine = null)
		{
			if (writeLine == null)
				writeLine = s => Debug.WriteLine(s);

			this.next = next;
			this.writeLine = writeLine;
		}

		public IDisposable ConfigureSteps(params int[] aSteps)
		{
			steps = new Step[aSteps.Length == 1 ? aSteps[0] : aSteps.Length];

			next.ConfigureSteps(aSteps);

			return new ActionDisposable(() =>
			{
				if (currentStep >= 0 && currentStep < steps.Length)
					Finished();
			});
		}

		public void StartStep(string stepName = null)
		{
			if (currentStep >= 0)
				OnFinishedStep();

			next.StartStep(stepName);

			currentStep++;
			steps[currentStep].Name = stepName;
			start = DateTime.Now;
		}

		public ProgressMonitor CreateSubMonitor()
		{
			return next.CreateSubMonitor();
		}

		public void Finished()
		{
			OnFinishedStep();

			next.Finished();

			DumpTimes();

			currentStep++;
		}

		private void OnFinishedStep()
		{
			steps[currentStep].Size = (int) Math.Round((DateTime.Now - start).TotalMilliseconds);
		}

		private void DumpTimes()
		{
			var max = steps.Select(s => s.Size)
				.Aggregate(Math.Max);
			var factor = 100.0 / max;

			for (int i = 0; i < steps.Length; i++)
				steps[i].Size = (int) Math.Max(Math.Round(steps[i].Size * factor), 1);

			var gcd = steps.Select(s => s.Size)
				.Aggregate(Utils.GetGCD);

			for (int i = 0; i < steps.Length; i++)
				steps[i].Size = steps[i].Size / gcd;

			for (int i = 0; i < steps.Length; i++)
				writeLine(string.Format("Step {0} - {1} : {2}", i, steps[i].Name, steps[i].Size));
		}

		public void Report(string message, params object[] args)
		{
			next.Report(message, args);
		}

		public void ReportDetail(string message, params object[] args)
		{
			next.ReportDetail(message, args);
		}

		public void ReportWarning(string message, params object[] args)
		{
			next.ReportWarning(message, args);
		}

		public void ReportError(string message, params object[] args)
		{
			next.ReportError(message, args);
		}

		private struct Step
		{
			public int Size;
			public string Name;
		}
	}
}
