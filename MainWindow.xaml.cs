using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace elect.net7_minigame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    /// Поле - двумерный массив, где каждая ячейка может быть:
    /// 0 - пустым
    /// 1 - тело змейки
    /// 2 - голова змейки
    /// 3 - еда
    public partial class MainWindow : Window
    {
        #region Constants
        private const int DefaultGridSize = 10; // Размер игрового поля (NxN)
        private const int DefaultCellSize = 20; // Размер одной ячейки в пикселях
        private const int DefaultSnakeLength = 3; // Начальная длина змейки
        private const int GameTickIntervalMs = 200; // Интервал обновления игры (мс)
        private const int FoodRegenIntervalMs = 3000; // Интервал появления новой еды (мс)
        private static readonly Brush SnakeBodyBrush = Brushes.Red; // Цвет тела змейки
        private static readonly Brush SnakeHeadBrush = Brushes.DarkRed; // Цвет головы змейки
        private static readonly Brush FoodBrush = Brushes.Green; // Цвет еды
        private static readonly Brush GridLineBrush = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)); // Линии сетки (~80% прозрачность)
        private static readonly Brush CellAltBrush1 = Brushes.White; // Цвет фона ячейки нечёт
        private static readonly Brush CellAltBrush2 = Brushes.LightGray; // Цвет фона ячейки чёт
        #endregion

        #region Fields
        private DispatcherTimer mainTimer; // Таймер для игрового цикла
        private DispatcherTimer foodTimer; // Таймер для появления еды
        private List<Point> snake; // Список точек, составляющих змейку
        private int snakeLength = DefaultSnakeLength; // Текущая длина змейки
        private Directions dir = Directions.right; // Текущее направление движения
        private int gridSize = DefaultGridSize; // Размер сетки
        private int cellSize = DefaultCellSize; // Размер ячейки
        private List<Point> foodPositions = new List<Point>(); // Список позиций еды
        private Random rng = new Random(); // Генератор случайных чисел
        private bool collisionDetected = false; // Флаг столкновения
        #endregion

        public enum Directions
        {
            up, down, left, right
        };


        public MainWindow()
        {
            InitializeComponent();
            // Создаём змейку в центре поля
            snake = new List<Point>
                {
                    new Point(gridSize / 2, gridSize / 2),
                    new Point(gridSize / 2 - 1, gridSize / 2),
                    new Point(gridSize / 2 - 2, gridSize / 2)
                };

            this.PreviewKeyDown += MainWindow_KeyDown;

            if (gameGrid != null)
            {
                for (int i = 0; i < gridSize; i++)
                {
                    gameGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellSize) });
                    gameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cellSize) });
                }
            }

            // отдельный таймер для генерации еды
            foodTimer = new DispatcherTimer();
            foodTimer.Interval = TimeSpan.FromMilliseconds(FoodRegenIntervalMs);
            foodTimer.Tick += (s, e) => { SpawnFood(); Tick_DrawingCore(); };
            foodTimer.Start();

            scoreText.Text = "Score: 0";
            statusText.Text = "Status: On-going";
        }


        private void Tick_DrawingCore()
        {
            gameGrid.Children.Clear();

            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    // чередование цвета фона
                    var rect = new Rectangle
                    {
                        Width = cellSize,
                        Height = cellSize,
                        Fill = ((x + y) % 2 == 0) ? CellAltBrush1 : CellAltBrush2,
                        Stroke = GridLineBrush,
                        StrokeThickness = 1
                    };
                    Grid.SetRow(rect, y);
                    Grid.SetColumn(rect, x);
                    gameGrid.Children.Add(rect);
                }
            }

            // отрисовка еды
            foreach (var food in foodPositions)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = cellSize * 0.8,
                    Height = cellSize * 0.8,
                    Fill = FoodBrush
                };
                Grid.SetRow(ellipse, (int)food.Y);
                Grid.SetColumn(ellipse, (int)food.X);
                gameGrid.Children.Add(ellipse);
            }

            // отрисовка змейки
            for (int i = 0; i < snake.Count; i++)
            {
                var part = snake[i];
                Rectangle rect = new Rectangle
                {
                    Height = cellSize * 0.9,
                    Width = cellSize * 0.9,
                    Fill = (i == 0) ? SnakeHeadBrush : SnakeBodyBrush
                };
                Grid.SetRow(rect, (int)part.Y);
                Grid.SetColumn(rect, (int)part.X);
                gameGrid.Children.Add(rect);
            }
        }

        private void MoveSnake()
        {
            Point head = snake[0];
            Point newHead = head;

            switch (dir)
            {
                case Directions.up: newHead = new Point(head.X, head.Y - 1); break;
                case Directions.down: newHead = new Point(head.X, head.Y + 1); break;
                case Directions.left: newHead = new Point(head.X - 1, head.Y); break;
                case Directions.right: newHead = new Point(head.X + 1, head.Y); break;
            }

            collisionDetected = IsCollision(newHead);

            if (collisionDetected)
            {
                mainTimer?.Stop(); // ?
                MessageBox.Show("Game Over!");
                return;
            }

            snake.Insert(0, newHead);

            var foodEaten = foodPositions.FirstOrDefault(f => f == newHead);
            if (foodEaten != default(Point))
            {
                snakeLength++;
                foodPositions.Remove(foodEaten);
            }

            while (snake.Count > snakeLength)
                snake.RemoveAt(snake.Count - 1);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                MoveSnake();
                Tick_DrawingCore();

                scoreText.Text = $"Score: {snakeLength - DefaultSnakeLength}";
                statusText.Text = collisionDetected ? "Status: Stopped" : "Status: On-going";
            }
            catch (Exception ex)
            {
                if (mainTimer.IsEnabled)
                {
                    mainTimer.Stop();
                }
                MessageBox.Show(ex.Message);
            }
        }


        // обработчик нажатий на клавиши
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W:
                    if (dir != Directions.down) dir = Directions.up;
                    break;
                case Key.S:
                    if (dir != Directions.up) dir = Directions.down;
                    break;
                case Key.D:
                    if (dir != Directions.left) dir = Directions.right;
                    break;
                case Key.A:
                    if (dir != Directions.right) dir = Directions.left;
                    break;
                default:
                    return;
            }
        }

        // логика коллизии
        private bool IsCollision(Point pt)
        {
            if (pt.X < 0 || pt.Y < 0 || pt.X >= gridSize || pt.Y >= gridSize)
                return true;

            return snake.Skip(1).Any(s => s == pt);
        }


        private void button_timer_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame(); // Сбросить игру

            if (mainTimer == null)
            {
                mainTimer = new DispatcherTimer();
                mainTimer.Interval = TimeSpan.FromMilliseconds(GameTickIntervalMs);
                mainTimer.Tick += Timer_Tick;
            }

            mainTimer.Start();
        }

        // стартуем по дефолту
        private void StartNewGame()
        {
            snakeLength = DefaultSnakeLength;
            dir = Directions.right;
            collisionDetected = false;
            foodPositions.Clear();

            snake = new List<Point>
            {
                new Point(gridSize / 2, gridSize / 2),
                new Point(gridSize / 2 - 1, gridSize / 2),
                new Point(gridSize / 2 - 2, gridSize / 2)
            };

            SpawnFood();
            Tick_DrawingCore();
            scoreText.Text = "Score: 0";
            statusText.Text = "Status: On-going";
        }

        private void SpawnFood()
        {
            // edgecase
            //if (foodPositions.Count + snake.Count >= gridSize * gridSize)
            //    return;

            Point pt;
            //int attempts = 0;
            do
            {
                pt = new Point(rng.Next(0, gridSize), rng.Next(0, gridSize));
                // edgecase
                //attempts++;
                //if (attempts > 100) return;
                // hehe
            }
            while (snake.Contains(pt) || foodPositions.Contains(pt));

            foodPositions.Add(pt);
        }
    }
}
