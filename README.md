# Get started

1. Clone the repo to your local machine
2. Configure start-up projects for the solution (right-click on .sln file in Solution Explorer)
		a. MessageRouter (first)
		b. MessageRetrieverAPI (second)
3. Configure Connected Services for the MessageRetrieverAPI project
    a. SQL Server Database => SQL Server Express LocalDB
    b. Azure Cosmos DB => provision a new instance (or select an existing one)
4. Configure Connected Services for the MessageRouter project
    a. Azure Cosmos DB => select the same instance as in step 3b
    b. Azure SignalR Service => provision a new instance (or select an existing one)
    c. RabbitMQ => create the queue named 'UnprocessedMessage' through the 'management portal' (click on the '...')
    
Note: the first time you fire up the MessageRetrieverAPI project it will create the database 

# Architecture

![image](https://user-images.githubusercontent.com/8742622/163857699-92be1e10-5a6b-467b-9341-5d83bff4a54b.png)


