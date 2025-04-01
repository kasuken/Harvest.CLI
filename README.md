# 🚀 Harvest CLI Wizard

Harvest CLI Wizard is an easy-to-use command-line interface tool designed to simplify logging your working hours into Harvest. Quickly insert time entries, select projects and tasks assigned to you, and seamlessly add additional hours with an intuitive wizard.

## 🌟 Features

- **Simple Time Entry**: Quickly log your hours through straightforward prompts.
- **Project & Task Selection**: Automatically retrieves projects and tasks assigned to your account.
- **Effortless Extension**: Easily add additional hours after each entry with the built-in wizard.

## 🔧 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)

## 🔑 Harvest Personal Access Token

To use the CLI, you'll need to create a Personal Access Token in Harvest:

1. Log in to your [Harvest Account](https://id.getharvest.com/).
2. Go to your profile icon (top-right corner) and select **Developers**.
3. Click on **Create New Personal Access Token**.
4. Give it a descriptive name (e.g., `Harvest CLI Wizard`).
5. Choose the required permissions (**Time Tracking**) and click **Create Personal Access Token**.
6. Copy the generated token securely. You won't be able to see it again!

## ⚙️ Configuration

Create an `appsettings.json` file in the root directory of your CLI application and insert the following content:

```json
{
  "Harvest": {
    "AccountId": "YOUR_ACCOUNT_ID",
    "AccessToken": "YOUR_PERSONAL_ACCESS_TOKEN"
  }
}
```

- Replace `YOUR_ACCOUNT_ID` with your Harvest Account ID (you can find this in your Harvest profile or URL).
- Replace `YOUR_PERSONAL_ACCESS_TOKEN` with the token you created.

## 🚀 Getting Started

### Running the CLI

#### From source code

Clone the repository:

```bash
git clone https://github.com/yourusername/harvest-cli-wizard.git
cd harvest-cli-wizard
```

Run the CLI:

```bash
dotnet run
```

#### From the Release folder

```bash
Harvest.CLI.exe
```

Follow the on-screen prompts to quickly log your time entries.

## 🤝 Contributing

Contributions are welcome! Feel free to submit a pull request or open an issue for suggestions, improvements, or bug fixes.

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

