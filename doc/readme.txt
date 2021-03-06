Новые возможности.
------------------
1. Добавлена возможность обращаться в одном запросе не только к разным базам данным,
   расположенным на одном экземпляре SQL Server, но и к связанным (linked) серверам.
2. Реализован сервер web api, позволяющий переводить запросы в терминах 1С в запросы
   на T-SQL, а также выполнять эти запросы при помощи любого web api клиента.
3. Используется новый парсер языка запросов, поддерживающий практически полный
   синтаксис SQL Server 2005-2016. Это означает, что поддерживаются как DDL, так и
   DML инструкции.
   Также возможно использование DMV, табличных переменных, оконных функций,
   табличных указаний (хинтов) и т.д. и т.п.
4. Использование нового парсера в перспективе позволяет добавлять в синтаксис языка
   запросов собственные функции, собственную логику выполнения запроса, а также
   выполнять программный анализ запроса по заданным критериям.

Как это работает.
-----------------
1. Выгружаем метаданные 1С при помощи обработки ConfigurationExporter83.epf в XML.
2. Настраиваем web сервер 1C#, который реализован на базе web сервера Kestrel
   (в основном описываем структуру каталогов сервера СУБД и указываем URL).
3. Выполняем запросы к web серверу 1C# при помощи web api 1C#.

Описание web api находится в файле "OneCSharp Scripting.postman_collection.json".
Файл можно импортировать в программу Postman.

Настройка web сервера 1C#.
--------------------------
1. Распаковать установочный архив 1C# в любой каталог.
2. Создать в этом каталоге каталог metadata.
3. Внутри каталога metadata создать каталог c именем сервера СУБД.
4. Выгрузить метаданные 1С обработкой ConfigurationExporter83.epf
   в файл xml. Назвать этот файл также как называется база данных 
   СУБД. Положить этот файл в созданный ранее каталог сервера.
5. В файле настроек web сервера 1C# appsettings.json в секции
   MetadataSettings повторить структуру каталогов и баз данных 
   СУБД. Файл настроек, чтобы долго не искать, можно открыть при 
   помощи show-web-server-settings.bat.
6. При необходимости настроить URL, по которому будет запускаться
   web сервер 1C#, в том же файле настроек appsettings.json.
7. Web cервер 1C# готов к работе. Его можно запустить при помощи
   файла run-web-server.bat.

Для облегчения понимания как настроить web сервер архив установки содержит
уже настроенный каталог для тестового окружения автора.
То же самое касается файла appsettings.json.

Системные требования.
---------------------
1. Web сервер 1C# скомпилирован для платформы win-x64.
2. Для подключения к SQL Server web сервер 1C# использует
   встроенную аутентификацию Windows. Следовательно запускать его 
   нужно под учётной записью, которая имеет доступ к серверу СУБД 
   и нужным базам данных.
3. Требуется установка .NET Core 3.1.3
4. Требуется установка .NET Framework 4.8

.NET качаем здесь: https://dotnet.microsoft.com/download