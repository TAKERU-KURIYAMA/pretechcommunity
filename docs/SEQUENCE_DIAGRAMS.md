# GitHub Discord Notifier シーケンス図

## 1. ユーザー認証フロー

### 1.1 新規登録

```mermaid
sequenceDiagram
    participant U as ユーザー
    participant F as Frontend
    participant API as Backend API
    participant DB as Database
    participant E as Email Service

    U->>F: 新規登録画面アクセス
    F->>U: 登録フォーム表示
    U->>F: メール・パスワード入力
    F->>API: POST /api/auth/register
    API->>API: パスワード検証
    API->>API: bcryptハッシュ生成
    API->>DB: ユーザー情報保存
    DB-->>API: 保存完了
    API->>API: JWT生成
    API->>E: 確認メール送信（将来実装）
    API-->>F: JWT + ユーザー情報
    F->>F: トークン保存
    F->>U: ダッシュボードへリダイレクト
```

### 1.2 ログイン

```mermaid
sequenceDiagram
    participant U as ユーザー
    participant F as Frontend
    participant API as Backend API
    participant DB as Database

    U->>F: ログイン画面アクセス
    F->>U: ログインフォーム表示
    U->>F: メール・パスワード入力
    F->>API: POST /api/auth/login
    API->>DB: ユーザー情報取得
    DB-->>API: ユーザーデータ
    API->>API: パスワード検証
    alt パスワード一致
        API->>API: JWT生成
        API->>API: RefreshToken生成
        API->>DB: RefreshToken保存
        API-->>F: JWT + RefreshToken
        F->>F: トークン保存
        F->>U: ダッシュボードへ
    else パスワード不一致
        API-->>F: 401 Unauthorized
        F->>U: エラー表示
    end
```

## 2. システム管理フロー

### 2.1 システム作成

```mermaid
sequenceDiagram
    participant U as ユーザー
    participant F as Frontend
    participant API as Backend API
    participant DB as Database
    participant D as Discord API

    U->>F: システム作成画面
    U->>F: 情報入力（名前、Discord情報）
    F->>API: POST /api/systems
    API->>API: JWT検証
    API->>D: Botトークン検証
    D-->>API: 検証結果
    alt トークン有効
        API->>DB: システム情報保存
        API->>DB: SystemMember作成（Owner）
        DB-->>API: 保存完了
        API-->>F: システム情報
        F->>U: システム詳細画面へ
    else トークン無効
        API-->>F: 400 Bad Request
        F->>U: エラー表示
    end
```

### 2.2 メンバー招待

```mermaid
sequenceDiagram
    participant O as オーナー/管理者
    participant F as Frontend
    participant API as Backend API
    participant DB as Database
    participant E as Email Service
    participant M as 招待されたメンバー

    O->>F: メンバー招待モーダル
    O->>F: メールアドレス入力
    F->>API: POST /api/systems/{id}/members
    API->>API: 権限チェック
    API->>DB: ユーザー存在確認
    alt ユーザー存在
        API->>DB: SystemMember作成
        API->>E: 招待通知メール
        API-->>F: 成功レスポンス
    else ユーザー未登録
        API->>DB: 招待レコード作成
        API->>E: 招待メール送信
        API-->>F: 招待送信完了
    end
    F->>O: 完了通知
    
    Note over M: 招待メール受信後
    M->>F: 招待リンククリック
    F->>API: GET /api/invitations/{token}
    API->>DB: 招待検証
    API-->>F: 招待情報
    F->>M: 登録/ログイン促進
```

## 3. GitHub連携フロー

### 3.1 リポジトリ追加とWebhook設定

```mermaid
sequenceDiagram
    participant U as ユーザー
    participant F as Frontend
    participant API as Backend API
    participant DB as Database
    participant GH as GitHub API

    U->>F: リポジトリ追加画面
    U->>F: リポジトリURL入力
    F->>API: POST /api/systems/{id}/repositories
    API->>API: 権限チェック
    API->>GH: リポジトリ情報取得
    GH-->>API: リポジトリ詳細
    API->>API: Webhook Secret生成
    API->>DB: リポジトリ情報保存
    DB-->>API: 保存完了
    API-->>F: リポジトリ情報 + Webhook設定
    F->>U: Webhook設定手順表示
    
    Note over U: GitHub側でWebhook設定
    U->>GH: Webhook設定画面
    U->>GH: Webhook URL・Secret入力
    GH->>API: Ping イベント送信
    API->>API: 署名検証
    API-->>GH: 200 OK
```

### 3.2 GitHub Webhook受信

```mermaid
sequenceDiagram
    participant GH as GitHub
    participant API as Backend API
    participant DB as Database
    participant DS as Discord Service
    participant D as Discord Server

    GH->>API: POST /api/webhook/{systemId}
    Note right of API: Headers: X-Hub-Signature-256
    API->>DB: System情報取得
    DB-->>API: WebhookSecret
    API->>API: HMAC-SHA256署名検証
    alt 署名検証成功
        API->>API: イベントタイプ判定
        API->>DB: 通知設定取得
        DB-->>API: 該当する通知チャンネル
        API->>API: テンプレート処理
        API->>DS: 通知送信依頼
        DS->>D: Discord APIで送信
        D-->>DS: 送信結果
        DS-->>API: 完了
        API->>DB: 通知履歴保存
        API-->>GH: 200 OK
    else 署名検証失敗
        API-->>GH: 401 Unauthorized
    end
```

## 4. Discord通知フロー

### 4.1 通知設定作成

```mermaid
sequenceDiagram
    participant U as ユーザー
    participant F as Frontend
    participant API as Backend API
    participant DB as Database
    participant D as Discord API

    U->>F: 通知設定画面
    U->>F: リポジトリ・チャンネル選択
    F->>API: GET /api/systems/{id}/discord/channels
    API->>D: チャンネル一覧取得
    D-->>API: チャンネルリスト
    API-->>F: 選択可能チャンネル
    F->>U: チャンネル選択UI
    U->>F: イベントタイプ・テンプレート設定
    F->>API: POST /api/systems/{id}/notifications
    API->>DB: 通知設定保存
    DB-->>API: 保存完了
    API-->>F: 通知設定情報
    F->>U: 設定完了通知
```

### 4.2 カスタムテンプレート処理

```mermaid
sequenceDiagram
    participant W as Webhook受信
    participant T as Template Engine
    participant DB as Database
    participant D as Discord Service

    W->>DB: 通知設定取得
    DB-->>W: テンプレート情報
    W->>T: テンプレート + イベントデータ
    T->>T: Handlebars解析
    T->>T: 変数置換
    T->>T: 条件分岐処理
    T-->>W: 生成されたメッセージ
    W->>D: Discord Embed作成
    D->>D: リッチメッセージ構築
    D-->>W: 送信準備完了
```

## 5. エラーハンドリングフロー

### 5.1 API エラー処理

```mermaid
sequenceDiagram
    participant C as Client
    participant API as Backend API
    participant L as Logger
    participant M as Monitoring

    C->>API: APIリクエスト
    API->>API: 処理実行
    alt エラー発生
        API->>L: エラーログ記録
        L->>M: アラート送信
        API->>API: エラーレスポンス生成
        API-->>C: エラーレスポンス
        Note right of C: {<br/>  "error": {<br/>    "code": "ERROR_CODE",<br/>    "message": "...",<br/>    "details": {...}<br/>  }<br/>}
    else 正常処理
        API-->>C: 成功レスポンス
    end
```

### 5.2 Discord送信失敗時のリトライ

```mermaid
sequenceDiagram
    participant W as Webhook Processor
    participant Q as Queue Service
    participant D as Discord Service
    participant DB as Database

    W->>Q: 通知タスク追加
    Q->>D: 通知送信試行
    alt 送信成功
        D-->>Q: 200 OK
        Q->>DB: 送信履歴記録
    else 送信失敗
        D-->>Q: エラー
        Q->>Q: リトライ回数確認
        alt リトライ可能
            Q->>Q: 待機（指数バックオフ）
            Q->>D: 再送信
        else リトライ上限
            Q->>DB: 失敗記録
            Q->>W: DLQ送信
        end
    end
```

## 6. 認証トークン更新フロー

```mermaid
sequenceDiagram
    participant F as Frontend
    participant API as Backend API
    participant DB as Database

    Note over F: JWTの有効期限切れ検出
    F->>F: RefreshToken取得
    F->>API: POST /api/auth/refresh
    API->>DB: RefreshToken検証
    alt 有効なRefreshToken
        API->>API: 新しいJWT生成
        API->>API: 新しいRefreshToken生成
        API->>DB: RefreshToken更新
        API-->>F: 新しいトークンペア
        F->>F: トークン保存
        F->>F: 元のリクエスト再実行
    else 無効なRefreshToken
        API-->>F: 401 Unauthorized
        F->>F: ログイン画面へリダイレクト
    end
```

## 7. システム削除フロー

```mermaid
sequenceDiagram
    participant O as オーナー
    participant F as Frontend
    participant API as Backend API
    participant DB as Database
    participant D as Discord Service

    O->>F: システム設定画面
    O->>F: 削除ボタンクリック
    F->>O: 確認ダイアログ表示
    O->>F: 削除確認
    F->>API: DELETE /api/systems/{id}
    API->>API: オーナー権限確認
    API->>DB: 関連データ取得
    API->>D: Bot切断
    API->>DB: NotificationChannels削除
    API->>DB: Repositories削除
    API->>DB: SystemMembers削除
    API->>DB: System削除
    DB-->>API: 削除完了
    API-->>F: 204 No Content
    F->>O: ダッシュボードへリダイレクト
```