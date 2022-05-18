## Библиотека для работы с RabbitMQ

Пример использования: https://github.com/Volirvag49/RabbitMqClient/tree/main/RabbitMQConnector.Tests
## Настройка:

### Docker
``` docker run --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.8-management ```

### Регистрируем сервис для отправки сообщений:
  ```cs
  
  public static IServiceCollection ConfigureServices()
  {
  
      // ...
     
      services.AddEventBusPublisher
      (
          "ConnectionUrl", // amqp://guest:guest@localhost:5672
          "ExchangeName", // RabbitMQConnector.Tests
          timeoutBeforeReconnecting // 15
      );

      // ...
  }

```
  
### Регистрируем сервис для получения сообщений:
  
```cs

  public static IServiceCollection ConfigureServices()
  {
  
      //...
      
      services.AddEventBusSubscriber
      (
          "ConnectionUrl", // amqp://guest:guest@localhost:5672
          "ExchangeName", // RabbitMQConnector.Tests
          timeoutBeforeReconnecting // 15
      );
   
      //...
  }

```

## Как пользоваться

### Отправка сообщений

```cs
  // Main
  public void Main(string[] args)
  {
      // В качестве рабочей нагрузки может выступать любой класс
      SamplePayload samplePayload = new SamplePayload
      {
          Content = Guid.NewGuid().ToString()
      };
      
      // Оборачиваем рабочую нагрузку в EventBusMessage
      EventBusMessage eventBusMessage =
          new EventBusMessage(samplePayload);
      
      // Публикуем сообщение
      string publishRoute = "SampleRoute"; // Роут
      _eventBusPublisher.Publish(publishRoute, eventBusMessage);
     
  }

  // Рабочая нагрузка
  public class SamplePayload
  {
      // Контент
      public string Content { get; set; }
  }

```

### Получение сообщений

```cs
  // Main
  public void Main(string[] args)
  {
      // Подписываемся на обработку сообщения
      string subscribeQueue = "SampleQueue"; // Очередь
      _eventBusSubscriber.Subscribe<SamplePayload>(subscribeQueue, ConsumerReceived);
  }


  // Событие получения сообщения
  private void ConsumerReceived(object sender, ConsumerReceivedEventArgs e)
  {
      // Получение рабочей нагрузки
      SamplePayload message = e.GetPayload<SamplePayload>();
      
      // ... логика...

      // Отмечаем сообщение как полученное
      e.Received();
  }

  // Рабочая нагрузка
  public class SamplePayload
  {
      // Контент
      public string Content { get; set; }
  }

```
