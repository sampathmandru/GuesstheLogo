using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;

namespace WiseWork.Services
{
    public class GameService
    {
        private readonly ILogger<GameService> _logger;
        private readonly NavigationManager _navigationManager;
        private HubConnection _hubConnection;
        private List<QuizImage> _quizImages = new List<QuizImage>();
        private int _currentQuestionIndex = 0;
        private int _timeLeft = 30;
        private int _totalScore = 0;
        private int _questionsAnswered = 0;
        private DateTime _gameEndTime;
        private const int GameDurationMinutes = 15;
        private Dictionary<string, int> _playerScores = new Dictionary<string, int>();
        private string _winnerName;
        private bool _gameEnded = false;

        public event Action OnStateChanged;

        public GameService(ILogger<GameService> logger, NavigationManager navigationManager)
        {
            _logger = logger;
            _navigationManager = navigationManager;
        }

        public async Task InitializeGame(string gamePin, string playerName)
        {
            await SetupSignalRConnection(gamePin, playerName);
            _gameEndTime = DateTime.Now.AddMinutes(GameDurationMinutes);
            NotifyStateChanged();
        }
        public async Task CreateGame(string gamePin)
        {
            await LoadQuestions();
            _gameEndTime = DateTime.Now.AddMinutes(GameDurationMinutes);
            NotifyStateChanged();
        }
        public HubConnection GetHubConnection() => _hubConnection;

        private async Task SetupSignalRConnection(string gamePin, string playerName)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_navigationManager.ToAbsoluteUri("/roomHub"))
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<Dictionary<string, int>>("UpdateScores", (scores) =>
            {
                _playerScores = scores;
                NotifyStateChanged();
            });

            _hubConnection.On<string>("WinnerAnnounced", (winnerName) =>
            {
                _winnerName = winnerName;
                _gameEnded = true;
                NotifyStateChanged();
            });

            _hubConnection.On<TimeSpan>("ReceiveGameTime", (timeRemaining) =>
            {
                _gameEndTime = DateTime.Now + timeRemaining;
                NotifyStateChanged();
            });

            await _hubConnection.StartAsync();
            await _hubConnection.SendAsync("JoinRoom", gamePin);
            await _hubConnection.SendAsync("UpdatePlayerName", gamePin, playerName);
        }

        private async Task LoadQuestions()
        {
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "quiz-questions.json");
            string jsonContent = await File.ReadAllTextAsync(jsonPath);
            var allQuestions = JsonSerializer.Deserialize<List<QuizImage>>(jsonContent);

            if (allQuestions == null)
            {
                _logger.LogError("Failed to load questions from JSON file.");
                return;
            }

            Random rng = new Random();
            _quizImages = allQuestions.OrderBy(_ => rng.Next()).ToList();

            foreach (var quizItem in _quizImages)
            {
                quizItem.Path = $"/quiz-images/{quizItem.imageName}";
            }
        }

        public async Task SubmitAnswer(string userAnswer)
        {
            var currentImage = _quizImages[_currentQuestionIndex];
            int matchRatio = FuzzySharp.Fuzz.Ratio(userAnswer.ToLower(), currentImage.answer.ToLower());
            bool isCorrect = matchRatio >= 80;

            if (isCorrect)
            {
                int score = CalculateScore();
                _totalScore += score;
                _questionsAnswered++;

                if (_totalScore >= 1000 && _winnerName == null)
                {
                    await _hubConnection.SendAsync("AnnounceWinner", _winnerName);
                }

                await _hubConnection.SendAsync("UpdateScore", _totalScore);
            }

            NotifyStateChanged();
        }

        public async Task NextQuestion()
        {
            _currentQuestionIndex++;
            if (_currentQuestionIndex >= _quizImages.Count)
            {
                _currentQuestionIndex = 0;
            }
            _timeLeft = 30;
            await _hubConnection.SendAsync("NextQuestion", _currentQuestionIndex);
            NotifyStateChanged();
        }


        private int CalculateScore()
        {
            if (_timeLeft > 25) return 100;
            if (_timeLeft > 20) return 80;
            if (_timeLeft > 15) return 60;
            if (_timeLeft > 10) return 40;
            return 20;
        }

        public void DecrementTimer()
        {
            if (_timeLeft > 0)
            {
                _timeLeft--;
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        public QuizImage GetCurrentImage() => _quizImages[_currentQuestionIndex];
        public int GetTimeLeft() => _timeLeft;
        public int GetTotalScore() => _totalScore;
        public int GetQuestionsAnswered() => _questionsAnswered;
        public DateTime GetGameEndTime() => _gameEndTime;
        public Dictionary<string, int> GetPlayerScores() => _playerScores;
        public string GetWinnerName() => _winnerName;
        public bool IsGameEnded() => _gameEnded;
    }

    public class QuizImage
    {
        public string imageName { get; set; }
        public string question { get; set; }
        public string answer { get; set; }
        public string Path { get; set; }
    }
}