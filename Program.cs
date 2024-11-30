using System;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using System.Text;
using Telegram.Bot.Types.Enums;
using PRTelegramBot.Helpers;

namespace Program
{
    public class KeyInfo
    {
        public string? Key { get; set; }
        public DateTime IssueDate { get; set; }
        public bool IsActivated { get; set; }
        public long UserId { get; set; } // ID пользователя, который активировал ключ
    }
    public class UserKeyInfo
    {
        public long UserId { get; set; }      // ID пользователя
        public string? EnteredKey { get; set; } // Ключ, который ввел пользователь
        public DateTime KeyDate { get; set; }  // Дата создания ключа
    }
    public class UserWaitState
    {
        public bool WaitId { get; set; }
        public bool WaitKey { get; set; }
        public bool WaitAdminIdAdd { get; set; }
        public bool WaitAdminIdDel { get; set; }
        public bool WaitIdChannel { get; set; }
    }
    class Program
    {
        static long groupId = -1002181162627, ChannelId = -1002272763004;
        static List<KeyInfo> keys = new List<KeyInfo>();
        static List<UserKeyInfo> userKeyLogs = new List<UserKeyInfo>();
        static Dictionary<long, UserWaitState> userWaitStates = new Dictionary<long, UserWaitState>();
        static ITelegramBotClient client = new TelegramBotClient("7650422582:AAFndJ39RJUAAtJykp__f6DmFlND_NJIgZg");
        static async Task Main(string[] args)
        {
            keys = LoadKeysFromFile("keys.txt");
            userKeyLogs = LoadUserKeysFromFile("user_keys.txt");
            adminIds = LoadAdminIds("admins.txt");
            LoadIdsFromFile("ids.txt");
            await SkipOldUpdates();
            StartExpiredKeyCheck(client, groupId, ChannelId);
            client.StartReceiving(Update, Error);
            await Task.Delay(-1);
        }

        // Список ID администраторов
        static List<long> adminIds = new List<long> { 1991980696, 1270527615 };

        //Булевый метод который возвращает айди админов
        static bool IsAdmin(long userId)
        {
            return adminIds.Contains(userId);
        }

        // Обработка полученных обновлений, которых может быть много :)
        async static Task Update(ITelegramBotClient client, Update update, CancellationToken token)
        {
            var message = update.Message;
            if (update.ChatJoinRequest != null)
            {
                var joinRequest = update.ChatJoinRequest;

                Console.WriteLine($"Запрос на вступление от {joinRequest.From.FirstName} ({joinRequest.From.Id})");
            }

            if (message == null)
                return;
            if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
            {
                return;
            }
            // Проверка на команду /start
            if (message.Text == "/start")
            {
                string commands = "/help - 🧠 Помощь\n" +
                                  "/GetHelp ❔ Как вступить в канал?\n" +
                                  "/about - ❔ Узнать о боте\n" +
                                  "/admin Команды для Администратора";

                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                        new KeyboardButton[] { "🧠 Помощь", "❔ Как вступить в канал?", "❔ Узнать о боте"}
                    })
                {
                    ResizeKeyboard = true
                };

                await client.SendMessage(message.Chat.Id,
                    "Новостройки Грозного \n" + $"Выберите, что вас интересует: \n\n{commands}",
                    replyMarkup: keyboard);
            }
            else
            {
                if (!userWaitStates.ContainsKey(message.Chat.Id))
                {
                    userWaitStates[message.Chat.Id] = new UserWaitState();
                }
                var userState = userWaitStates[message.Chat.Id];

                switch (message.Text)
                {
                    case "/about":
                    case "❔ Узнать о боте":
                        await client.SendMessage(message.Chat.Id, "@Ahl_As_Suffa и @deniamhdv");
                        break;
                    case "/list":
                    case "Список Участников":
                        if (!IsAdmin(message.Chat.Id))
                            return;
                        await client.SendMessage(message.Chat.Id, "Скоро");
                        break;
                    case "/help":
                    case "🧠 Помощь":
                        await client.SendMessage(message.Chat.Id, "/help - 🧠 Помощь\n" +
                                  "/GetHelp ❔ Как вступить в канал?\n" +
                                  "/about - ❔ Узнать о боте\n" +
                                  "/admin 👨‍💻 Для Администраторов");
                        break;
                    case "/setid":
                        if (!IsAdmin(message.Chat.Id))
                            return;
                        await client.SendMessage(message.Chat.Id, "Введите ID вашей группы: ");
                        userState.WaitId = true;
                        userState.WaitIdChannel = false;
                        break;
                    case "/setidChannel":
                        if (!IsAdmin(message.Chat.Id))
                            return;
                        await client.SendMessage(message.Chat.Id, "Введите ID вашего канала: ");
                        userState.WaitIdChannel = true;
                        userState.WaitId = false;
                        break;
                    case "/GetHelp":
                    case "❔ Как вступить в канал?":
                        await client.SendMessage(message.Chat.Id, "🚀 Вступить в канал просто, нужно лишь:\n" +
                                  "1) 🚅 Отправить запрос на вступление в канал: https://t.me/+SwvHUT2HJ1Q0OGEy \n" +
                                  "2) 🚅 Отправить запрос на вступление в группу: https://t.me/+3GbB6vRkhLIxNzNi \n" +
                                  "3) 🔑 Получить ключ у владельца\n" +
                                  "4) 🔐 Ввести ключ в данного бота, используя команду: '/activate' или '/активировать'\n\n" +
                                  " ✅ Если вы все сделаете правильно, наш бот автоматически примет ваш запрос на вступление в канал!");
                        break;
                    case "/admin":
                        if (!IsAdmin(message.Chat.Id))
                            return;

                        await client.SendMessage(message.Chat.Id, "/setid Установить новый ID группы\n" +
                                  "/setidChannel Установить новый ID канала\n" +
                                  "/list Список Участников\n" +
                                  "/getkey Сгенерировать Ключ\n" +
                                  "/addAdmin Добавить Администратора\n" +
                                  "/delAdmin Удалить Администратора\n" +
                                  "/adminsList Список Администраторов\n" +
                                  "/about - ❔ Узнать о боте");
                        break;
                    case "/getkey":
                        if (!IsAdmin(message.Chat.Id))
                            return;

                        var generatedKey = GenerateKey(16);
                        await client.SendMessage(message.Chat.Id, "Ключ: " + generatedKey);
                        var newKeyInfo = new KeyInfo
                        {
                            Key = generatedKey,
                            IssueDate = DateTime.Now,
                            IsActivated = false
                        };
                        keys.Add(newKeyInfo);
                        SaveKeysToFile(keys, "keys.txt");
                        break;
                    case "/activate":
                    case "/активировать":
                        await client.SendMessage(message.Chat.Id, "Введите ваш ключ в чат: ");
                        userState.WaitKey = true;
                        break;
                    case "/addAdmin":
                        if (!IsAdmin(message.Chat.Id))
                            return;

                        await client.SendMessage(message.Chat.Id, "Введите ID пользователя в чат: ");
                        userState.WaitAdminIdAdd = true;
                        userState.WaitAdminIdDel = false;
                        break;
                    case "/delAdmin":
                        if (!IsAdmin(message.Chat.Id))
                            return;

                        await client.SendMessage(message.Chat.Id, "Введите ID Администратора которого хотите удалить в чат: ");
                        userState.WaitAdminIdDel = true;
                        userState.WaitAdminIdAdd = false;
                        break;
                    case "/adminsList":
                        if (!IsAdmin(message.Chat.Id))
                            return;

                        await client.SendMessage(message.Chat.Id, "Список Администраторов: ");
                        for (int i = 0; i < adminIds.Count; i++)
                        {
                            await client.SendMessage(message.Chat.Id, "ID: " + adminIds[i].ToString());
                        }
                        break;
                    default:
                        if (!userState.WaitId && !userState.WaitKey && !userState.WaitAdminIdAdd && !userState.WaitAdminIdDel && !userState.WaitIdChannel)
                        {
                            await client.SendMessage(message.Chat.Id, "Я вас не понял. Может, посмотрим команды, которые я понимаю? /help");
                        }
                        else if (userState.WaitKey)
                        {
                            if (message.Text == null)
                                return;
                            // Получаем введенный ключ
                            string enteredKey = message.Text;

                            // Проверяем ключ
                            var keyInfo = keys.FirstOrDefault(k => k.Key == enteredKey && !k.IsActivated);
                            if (keyInfo == null)
                            {
                                await client.SendMessage(message.Chat.Id, "Неверный или уже использованный ключ.\n" + "Проверьте правильность написания вашего ключа!");
                                Console.WriteLine("использованный ключ");
                                break;
                            }
                            // Принимаем заявку на вступление
                            try
                            {
                                await client.ApproveChatJoinRequest(
                                    chatId: groupId,
                                    userId: message.Chat.Id
                                );
                                await client.ApproveChatJoinRequest(
                                    chatId: ChannelId,
                                    userId: message.Chat.Id
                                );
                                // Активируем ключ
                                keyInfo.IsActivated = true;
                                keyInfo.IssueDate = DateTime.Now;
                                keyInfo.UserId = message.Chat.Id;

                                // Добавляем запись о пользователе
                                userKeyLogs.Add(new UserKeyInfo
                                {
                                    UserId = message.Chat.Id,
                                    EnteredKey = enteredKey,
                                    KeyDate = keyInfo.IssueDate
                                });
                                // Сохраняем данные
                                SaveKeysToFile(keys, "keys.txt");
                                SaveUserKeysToFile(userKeyLogs, "user_keys.txt");
                                await client.SendMessage(message.Chat.Id, "Ключ успешно активирован!.\n" + "Вы были добавлены на канал и в группу.");
                            }
                            catch (Exception ex)
                            {
                                await client.SendMessage(message.Chat.Id, $"Ключ не активирован, проверьте, отправили ли вы запрос на вступление. {ex.Message} \n" + "В противном случае обратитесь к специалистам: @Ahl_As_Suffa и @deniamhdv");
                            }
                            userState.WaitKey = false;
                        }
                        else if (userState.WaitId)
                        {
                            if (long.TryParse(message.Text, out long parsedGroupId))
                            {
                                groupId = parsedGroupId;
                                userState.WaitId = false;
                                await client.SendMessage(message.Chat.Id, $"ID группы установлен: {groupId}");
                                SaveIdsToFile("ids.txt");
                            }
                            else
                            {
                                await client.SendMessage(message.Chat.Id, "Ошибка: введённое значение не является допустимым ID группы.");
                            }
                        }
                        else if (userState.WaitAdminIdAdd)
                        {
                            if (long.TryParse(message.Text, out long userId))
                            {
                                adminIds.Add(userId);
                                SaveAdminIds(adminIds, "admins.txt");
                                userState.WaitAdminIdAdd = false;
                                await client.SendMessage(message.Chat.Id, $"Администратор успешно добавлен! ID: " + userId);
                            }
                            else
                            {
                                await client.SendMessage(message.Chat.Id, "Ошибка: введённое значение не является допустимым ID пользователя.");
                            }
                        }
                        else if (userState.WaitAdminIdDel)
                        {
                            if (long.TryParse(message.Text, out long userId))
                            {
                                if (adminIds.Contains(userId))
                                {
                                    adminIds.Remove(userId);
                                    SaveAdminIds(adminIds, "adminIds.txt");
                                    userState.WaitAdminIdDel = false;
                                    await client.SendMessage(message.Chat.Id, $"Администратор успешно Удален! ID: " + userId);
                                }
                                else
                                {
                                    await client.SendMessage(message.Chat.Id, $"Такой Администратор не найден! ID: " + userId);
                                }
                            }
                            else
                            {
                                await client.SendMessage(message.Chat.Id, "Ошибка: введённое значение не является допустимым ID пользователя.");
                            }
                        }
                        else if (userState.WaitIdChannel)
                        {
                            if (long.TryParse(message.Text, out long parsedChannelId))
                            {
                                ChannelId = parsedChannelId;
                                userState.WaitIdChannel = false;
                                await client.SendMessage(message.Chat.Id, $"ID канала установлен: {ChannelId}");
                                SaveIdsToFile("ids.txt");
                            }
                            else
                            {
                                await client.SendMessage(message.Chat.Id, "Ошибка: введённое значение не является допустимым ID канала.");
                            }
                        }
                        break;
                }
            }
        }

        // Обработка ошибок которых не должно быть в проекте :D
        private static Task Error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }

        // Короче, генерация ключей
        static string GenerateKey(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder keyBuilder = new StringBuilder();
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                keyBuilder.Append(chars[random.Next(chars.Length)]);
            }

            return keyBuilder.ToString();
        }

        // Короче, сохранение ключей в файл
        static void SaveKeysToFile(List<KeyInfo> keys, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false)) // false - перезаписывать файл
                {
                    foreach (var keyInfo in keys)
                    {
                        writer.WriteLine($"{keyInfo.Key},{keyInfo.IssueDate},{keyInfo.IsActivated},{keyInfo.UserId}");
                    }
                }
                Console.WriteLine("Ключи успешно сохранены в файл.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении ключей в файл: {ex.Message}");
            }
        }

        // Короче, загрузка ключей из файла
        static List<KeyInfo> LoadKeysFromFile(string filePath)
        {
            List<KeyInfo> loadedKeys = new List<KeyInfo>();

            try
            {
                if (!System.IO.File.Exists(filePath)) return loadedKeys; // Если файл отсутствует, возвращаем пустой список

                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');

                    if (parts.Length < 4) continue; // Пропускаем некорректные строки

                    string key = parts[0]; // Первый элемент - ключ
                    DateTime issueDate = DateTime.Parse(parts[1]); // Второй элемент - дата выдачи
                    bool isActivated = bool.Parse(parts[2]); // Третий элемент - статус активации
                    long userId = long.Parse(parts[3]); // Четвертый элемент - ID пользователя

                    // Добавляем объект KeyInfo в список
                    loadedKeys.Add(new KeyInfo
                    {
                        Key = key,
                        IssueDate = issueDate,
                        IsActivated = isActivated,
                        UserId = userId
                    });
                }

                Console.WriteLine("Ключи успешно загружены из файла.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке ключей из файла: {ex.Message}");
            }

            return loadedKeys;
        }

        // Короче, сохранение данных про пользователей
        static void SaveUserKeysToFile(List<UserKeyInfo> userKeyLogs, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false)) // false - перезаписывать файл
                {
                    foreach (var log in userKeyLogs)
                    {
                        writer.WriteLine($"{log.UserId},{log.EnteredKey},{log.KeyDate}");
                    }
                }
                Console.WriteLine("Данные пользователей успешно сохранены в файл.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении данных пользователей в файл: {ex.Message}");
            }
        }

        // Короче, загрузка данных про пользователей
        static List<UserKeyInfo> LoadUserKeysFromFile(string filePath)
        {
            List<UserKeyInfo> loadedLogs = new List<UserKeyInfo>();

            try
            {
                if (!System.IO.File.Exists(filePath)) return loadedLogs; // Если файл отсутствует, возвращаем пустой список

                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length < 3) continue; // Пропускаем некорректные строки

                    long userId = long.Parse(parts[0]);
                    string enteredKey = parts[1];
                    DateTime keyDate = DateTime.Parse(parts[2]);

                    loadedLogs.Add(new UserKeyInfo
                    {
                        UserId = userId,
                        EnteredKey = enteredKey,
                        KeyDate = keyDate
                    });
                }

                Console.WriteLine("Данные пользователей успешно загружены из файла.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных пользователей из файла: {ex.Message}");
            }

            return loadedLogs;
        }
        private static Timer? _expiredKeyTimer;
        // Короче, таймер
        public static void StartExpiredKeyCheck(ITelegramBotClient client, long groupId, long ChannelId)
        {
            // Проверка будет запускаться каждые 24 часа (86400000 миллисекунд)
            _expiredKeyTimer = new Timer(async _ =>
            {
                await KickExpiredUsers(client, groupId, ChannelId);
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }
        // Короче, бан за просроченный ключ, а чо он
        public static async Task KickExpiredUsers(ITelegramBotClient client, long chatId, long GroupId)
        {
            // Находим истёкшие ключи
            var expiredKeys = keys.Where(keyInfo =>
                keyInfo.IsActivated &&
                (DateTime.Now - keyInfo.IssueDate).TotalMinutes >= 1).ToList();
            foreach (var keyInfo in expiredKeys)
            {
                try
                {
                    // Кик из группы
                    await client.BanChatMember(chatId, keyInfo.UserId);
                    await client.UnbanChatMember(chatId, keyInfo.UserId);

                    // Кик из канала
                    await client.BanChatMember(GroupId, keyInfo.UserId);
                    await client.UnbanChatMember(GroupId, keyInfo.UserId);

                    Console.WriteLine($"Пользователь с ID {keyInfo.UserId} был кикнут.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при кике пользователя с ID {keyInfo.UserId}: {ex.Message}");
                }
            }

            // Удаление всех просроченных ключей
            keys.RemoveAll(keyInfo => expiredKeys.Contains(keyInfo));
            SaveKeysToFile(keys, "keys.txt");
            keys = LoadKeysFromFile("keys.txt");
        }

        // Короче, сохранение файла с админами
        public static void SaveAdminIds(List<long> adminIds, string filePath)
        {
            // Записываем список в файл
            try
            {
                System.IO.File.WriteAllLines(filePath, adminIds.Select(id => id.ToString()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении adminIds: {ex.Message}");
            }
        }

        // Короче, загрузка файла с админами
        public static List<long> LoadAdminIds(string filePath)
        {
            var adminIds = new List<long>();

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    var lines = System.IO.File.ReadAllLines(filePath);
                    adminIds = lines.Select(line => long.Parse(line)).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке adminIds: {ex.Message}");
            }

            return adminIds;
        }

        public static void SaveIdsToFile(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false)) // false для перезаписи файла
                {
                    writer.WriteLine($"groupId={groupId}");
                    writer.WriteLine($"ChannelId={ChannelId}");
                }
                Console.WriteLine("IDs успешно сохранены в файл.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
            }
        }
        public static void LoadIdsFromFile(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    var lines = System.IO.File.ReadAllLines(filePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            if (parts[0] == "groupId")
                                groupId = long.Parse(parts[1]);
                            else if (parts[0] == "ChannelId")
                                ChannelId = long.Parse(parts[1]);
                        }
                    }
                    Console.WriteLine("IDs успешно загружены из файла.");
                }
                else
                {
                    Console.WriteLine("Файл не найден.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке: {ex.Message}");
            }
        }
        private static async Task SkipOldUpdates()
        {
            try
            {
                var updates = await client.GetUpdates();
                if (updates.Any())
                {
                    // Устанавливаем offset на следующий после последнего UpdateId
                    var lastUpdateId = updates.Last().Id;
                    await client.GetUpdates(offset: lastUpdateId + 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при установке offset: {ex.Message}");
            }
        }
    }
}