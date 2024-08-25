/*
Copyright (c) 2024 みけCAT

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using TrainCrew;

class GetPower: Form
{
	private const int fontSize = 16, gridSize = 24;

	private struct Row
	{
		public Label NumLabel;
		public Label AmpareLabel;
		public Label WattHourLabel;
		public double WattHour;

		public Row(Control parent, string text, int y)
		{
			NumLabel = new Label();
			NumLabel.Location = new Point(0, y);
			NumLabel.Size = new Size(gridSize * 2, gridSize);
			NumLabel.TextAlign = ContentAlignment.MiddleLeft;
			NumLabel.Text = text;
			parent.Controls.Add(NumLabel);
			AmpareLabel = new Label();
			AmpareLabel.Location = new Point(gridSize * 2, y);
			AmpareLabel.Size = new Size(gridSize * 4, gridSize);
			AmpareLabel.TextAlign = ContentAlignment.MiddleRight;
			AmpareLabel.Text = "--- A";
			parent.Controls.Add(AmpareLabel);
			WattHourLabel = new Label();
			WattHourLabel.Location = new Point(gridSize * 6, y);
			WattHourLabel.Size = new Size(gridSize * 8, gridSize);
			WattHourLabel.TextAlign = ContentAlignment.MiddleRight;
			WattHourLabel.Text = "0 Wh";
			parent.Controls.Add(WattHourLabel);
			WattHour = 0;
		}
	}

	public static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Run(new GetPower());
	}

	public GetPower()
	{
		bool trainCrewActive = false;
		int maxNumOfCars = 10;

		this.Font = new Font("MS UI Gothic", fontSize, GraphicsUnit.Pixel);
		this.ClientSize = new Size(gridSize * 15, gridSize * (maxNumOfCars + 4));
		this.FormBorderStyle = FormBorderStyle.FixedSingle;
		this.MaximizeBox = false;
		this.Text = "電力量計";
		Panel mainPanel = new Panel();
		mainPanel.Size = new Size(gridSize * 14, gridSize * (maxNumOfCars + 3));
		mainPanel.Location = new Point(gridSize / 2, gridSize / 2);
		this.Controls.Add(mainPanel);

		Button resetButton = new Button();
		resetButton.Location = new Point(0, 0);
		resetButton.Size = new Size(gridSize * 3, gridSize);
		resetButton.Text = "リセット";
		mainPanel.Controls.Add(resetButton);
		CheckBox ignoreMinusCheck = new CheckBox();
		ignoreMinusCheck.Location = new Point(gridSize * 3 + gridSize / 2, 0);
		ignoreMinusCheck.Size = new Size(gridSize * 6, gridSize);
		ignoreMinusCheck.Text = "負の電流を無視";
		mainPanel.Controls.Add(ignoreMinusCheck);
		CheckBox onlyWithPowerCheck = new CheckBox();
		onlyWithPowerCheck.Location = new Point(gridSize * 9 + gridSize / 2, 0);
		onlyWithPowerCheck.Size = new Size(gridSize * 4 + gridSize / 2, gridSize);
		onlyWithPowerCheck.Text = "力行時のみ";
		mainPanel.Controls.Add(onlyWithPowerCheck);

		Row summaryRow = new Row(mainPanel, "計", gridSize + gridSize / 2);
		summaryRow.AmpareLabel.Text = "";

		Row[] rows = new Row[maxNumOfCars];
		for (int i = 0; i < maxNumOfCars; i++)
		{
			rows[i] = new Row(mainPanel, string.Format("#{0}", i + 1), gridSize * (i + 3));
		}

		resetButton.Click += (s, e) => {
			for (int i = 0; i < maxNumOfCars; i++)
			{
				rows[i].WattHour = 0;
			}
		};

		TimeSpan? prevTime = null;

		TrainCrewInput.Init();
		trainCrewActive = true;
		Timer timer = new Timer();
		timer.Interval = 15;
		timer.Tick += (s, e) => {
			if (!trainCrewActive) return;
			TrainState trainState = TrainCrewInput.GetTrainState();
			GameScreen screen = TrainCrewInput.gameState.gameScreen;
			bool isGameScreen = screen == GameScreen.MainGame || screen == GameScreen.MainGame_Pause;
			TimeSpan? currentTime = isGameScreen ? (TimeSpan?)trainState.NowTime : null;
			double wattHourSum = 0;
			for (int i = 0; i < maxNumOfCars; i++)
			{
				if (i < trainState.CarStates.Count)
				{
					float volt = 1500;
					float ampare = trainState.CarStates[i].Ampare;
					rows[i].AmpareLabel.Text = string.Format("{0:F1} A", ampare);
					if (prevTime.HasValue && currentTime.HasValue &&
						(!ignoreMinusCheck.Checked || ampare >= 0) &&
						(!onlyWithPowerCheck.Checked || trainState.Pnotch > 0))
					{
						double watt = volt * ampare;
						double wattHourDelta = watt * (currentTime.Value.TotalHours - prevTime.Value.TotalHours);
						rows[i].WattHour += wattHourDelta;
					}
				}
				else
				{
					rows[i].AmpareLabel.Text = "--- A";
				}
				rows[i].WattHourLabel.Text = string.Format("{0:N1} Wh", rows[i].WattHour);
				wattHourSum += rows[i].WattHour;
			}
			summaryRow.WattHourLabel.Text = string.Format("{0:N1} Wh", wattHourSum);
			prevTime = currentTime;
		};
		timer.Start();
		FormClosed += (s, e) => {
			trainCrewActive = false;
			timer.Stop();
			TrainCrewInput.Dispose();
		};
	}
}
