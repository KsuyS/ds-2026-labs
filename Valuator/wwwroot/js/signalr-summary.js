const myTextId = new URLSearchParams(location.search).get('id');

if (myTextId) {
    const hubUrl = `${location.protocol}//${location.host}/notificationHub`;
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

    connection.on("ReceiveResult", (data) => {
        document.getElementById('rankValue').textContent = data.rank.toFixed(2);
    });

    connection.start().then(() =>
        connection.invoke('SubscribeToText', myTextId)
    );
}