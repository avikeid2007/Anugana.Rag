# Privacy Policy for Anugana.Rag

**Last Updated:** July 23, 2026

## 1. Introduction

**Anugana.Rag** ("the Application") is an open-source, cross-platform Retrieval-Augmented Generation (RAG) AI assistant application built using Uno Platform, .NET 10, Qdrant Vector Database, and OpenRouter AI. 

We respect your privacy and are committed to protecting your data. This Privacy Policy explains how data is handled when you use the Application.

---

## 2. Information Handling and Data Storage

### A. Local Device Data
- **Settings & API Keys**: All user configuration parameters—including API keys, custom endpoint URLs, model choices, search thresholds, and theme preferences—are stored locally on your device in secure local application storage.
- **Document Processing**: Documents (PDFs, text, markdown files) selected for ingestion are processed locally on your device to create text chunks and vector embeddings.

### B. Telemetry & Analytics
- The Application **does not collect, harvest, or track** any personal data, analytics, crash logs, or usage statistics.
- There are no third-party tracking SDKs or advertising identifiers integrated into the Application.

---

## 3. Third-Party Services & API Interactions

When using features of the Application, data is transmitted strictly to the AI and Database endpoints configured by you:

### A. Vector Database (Qdrant)
- Document text chunks and vector embeddings generated during ingestion are sent to your configured **Qdrant Vector Database** instance (Cloud or Local).
- Communication occurs via standard encrypted protocols (HTTPS/gRPC).

### B. AI Providers (OpenRouter / OpenAI / Local Models)
- When you send a chat message, the prompt along with relevant document context snippets retrieved from your vector database are sent to your configured AI inference API (such as **OpenRouter** or local Ollama endpoints) to generate streaming responses.
- Please refer to [OpenRouter's Privacy Policy](https://openrouter.ai/privacy) and [Qdrant's Privacy Policy](https://qdrant.tech/privacy-policy/) for details on how their respective cloud APIs process queries.

---

## 4. Children's Privacy

The Application does not knowingly collect or solicit any personal information from children under the age of 13. 

---

## 5. Changes to This Privacy Policy

We may update our Privacy Policy from time to time. Any changes will be posted directly in this repository with an updated revision date.

---

## 6. Contact Us

If you have any questions or concerns regarding this Privacy Policy, please open an issue on our official GitHub repository:
- **GitHub Repository**: [https://github.com/avikeid2007/Anugana.Rag](https://github.com/avikeid2007/Anugana.Rag)
