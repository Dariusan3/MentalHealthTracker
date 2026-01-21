// Functions for drawing charts

// Define chartUtils namespace
window.chartUtils = {
  // Function to draw mood trend chart
  createMoodTrendChart: function (containerId, dates, moodValues) {
    console.log("Call createMoodTrendChart with:", {
      containerId,
      dates,
      moodValues,
    });

    // Check if we have necessary data
    if (
      !dates ||
      !moodValues ||
      dates.length === 0 ||
      moodValues.length === 0
    ) {
      console.error("Insufficient data for drawing trend chart");
      return;
    }

    // Check if container element exists
    const chartContainer = document.getElementById(containerId);
    console.log("Container found:", chartContainer);

    if (!chartContainer) {
      console.error(`Container element for chart (${containerId}) not found`);

      // Check all elements with IDs in page for diagnostics
      const allElementsWithId = document.querySelectorAll("[id]");
      console.log(
        "Available elements with IDs:",
        Array.from(allElementsWithId).map((el) => el.id),
      );

      // Check if page is fully loaded
      console.log("Document ready state:", document.readyState);

      // Try again after a short delay
      setTimeout(() => {
        const retryContainer = document.getElementById(containerId);
        console.log("Retry finding container after delay:", retryContainer);

        if (retryContainer) {
          console.log("Container found on second attempt, drawing chart");
          this.createMoodTrendChartInternal(retryContainer, dates, moodValues);
        }
      }, 1000);

      return;
    }

    // Check if Chart.js is available
    if (typeof Chart === "undefined") {
      // Load Chart.js if not available
      console.log("Loading Chart.js...");
      const script = document.createElement("script");
      script.src = "https://cdn.jsdelivr.net/npm/chart.js";
      script.onload = function () {
        console.log("Chart.js loaded successfully");
        // After loading, draw the chart
        chartUtils.createMoodTrendChartInternal(
          chartContainer,
          dates,
          moodValues,
        );
      };
      document.head.appendChild(script);
    } else {
      console.log("Chart.js is already available, drawing chart");
      // Chart.js is already loaded, draw the chart
      this.createMoodTrendChartInternal(chartContainer, dates, moodValues);
    }
  },

  // Internal function for creating trend chart
  createMoodTrendChartInternal: function (container, dates, moodValues) {
    // Clear container in case a chart already exists
    container.innerHTML = "";
    const canvas = document.createElement("canvas");
    container.appendChild(canvas);

    // Create chart context
    const ctx = canvas.getContext("2d");

    // Define colors for different mood levels
    const getColorForMood = (value) => {
      if (value <= 3) return "rgba(244, 67, 54, 0.7)"; // Red for negative moods
      if (value <= 5) return "rgba(255, 152, 0, 0.7)"; // Orange for low neutral moods
      if (value <= 7) return "rgba(3, 169, 244, 0.7)"; // Blue for high neutral moods
      return "rgba(76, 175, 80, 0.7)"; // Green for positive moods
    };

    // Generate colors for each data point
    const pointColors = moodValues.map(getColorForMood);

    // Create chart
    new Chart(ctx, {
      type: "line",
      data: {
        labels: dates,
        datasets: [
          {
            label: "Mood Level",
            data: moodValues,
            borderColor: "rgba(75, 192, 192, 1)",
            backgroundColor: "rgba(75, 192, 192, 0.2)",
            tension: 0.4,
            pointBackgroundColor: pointColors,
            pointBorderColor: pointColors,
            pointRadius: 6,
            pointHoverRadius: 8,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          y: {
            min: 1,
            max: 10,
            ticks: {
              stepSize: 1,
            },
            title: {
              display: true,
              text: "Mood Level (1-10)",
            },
          },
          x: {
            title: {
              display: true,
              text: "Date",
            },
          },
        },
        plugins: {
          tooltip: {
            callbacks: {
              label: function (context) {
                const value = context.parsed.y;
                let label = "Level: " + value;

                // Add description for level
                if (value <= 3) label += " (Negative)";
                else if (value <= 5) label += " (Low Neutral)";
                else if (value <= 7) label += " (High Neutral)";
                else label += " (Positive)";

                return label;
              },
            },
          },
        },
      },
    });
  },
};

// Function to draw mood evolution chart
window.renderMoodChart = function (dates, moodValues) {
  // Check if we have necessary data
  if (!dates || !moodValues || dates.length === 0 || moodValues.length === 0) {
    console.error("Insufficient data for drawing chart");
    return;
  }

  // Check if container element exists
  const chartContainer = document.getElementById("moodChart");
  if (!chartContainer) {
    console.error("Container element for chart not found");
    return;
  }

  // Check if Chart.js is available
  if (typeof Chart === "undefined") {
    // Load Chart.js if not available
    console.log("Loading Chart.js...");
    const script = document.createElement("script");
    script.src = "https://cdn.jsdelivr.net/npm/chart.js";
    script.onload = function () {
      // After loading, draw the chart
      createMoodChart(chartContainer, dates, moodValues);
    };
    document.head.appendChild(script);
  } else {
    // Chart.js is already loaded, draw the chart
    createMoodChart(chartContainer, dates, moodValues);
  }
};

// Internal function for creating chart
function createMoodChart(container, dates, moodValues) {
  // Clear container in case a chart already exists
  container.innerHTML = "";
  const canvas = document.createElement("canvas");
  container.appendChild(canvas);

  // Create chart context
  const ctx = canvas.getContext("2d");

  // Define colors for different mood levels
  const getColorForMood = (value) => {
    if (value <= 3) return "rgba(244, 67, 54, 0.7)"; // Red for negative moods
    if (value <= 5) return "rgba(255, 152, 0, 0.7)"; // Orange for low neutral moods
    if (value <= 7) return "rgba(3, 169, 244, 0.7)"; // Blue for high neutral moods
    return "rgba(76, 175, 80, 0.7)"; // Green for positive moods
  };

  // Generate colors for each data point
  const pointColors = moodValues.map(getColorForMood);

  // Create chart
  new Chart(ctx, {
    type: "line",
    data: {
      labels: dates,
      datasets: [
        {
          label: "Mood Level",
          data: moodValues,
          borderColor: "rgba(75, 192, 192, 1)",
          backgroundColor: "rgba(75, 192, 192, 0.2)",
          tension: 0.4,
          pointBackgroundColor: pointColors,
          pointBorderColor: pointColors,
          pointRadius: 6,
          pointHoverRadius: 8,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      scales: {
        y: {
          min: 1,
          max: 10,
          ticks: {
            stepSize: 1,
          },
          title: {
            display: true,
            text: "Mood Level (1-10)",
          },
        },
        x: {
          title: {
            display: true,
            text: "Date",
          },
        },
      },
      plugins: {
        tooltip: {
          callbacks: {
            label: function (context) {
              const value = context.parsed.y;
              let label = "Level: " + value;

              // Add description for level
              if (value <= 3) label += " (Negative)";
              else if (value <= 5) label += " (Low Neutral)";
              else if (value <= 7) label += " (High Neutral)";
              else label += " (Positive)";

              return label;
            },
          },
        },
      },
    },
  });
}
