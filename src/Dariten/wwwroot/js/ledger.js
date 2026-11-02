const DB_NAME = "dariten";
const DB_VERSION = 1;
const STORES = ["accounts", "categories", "transactions", "budgets"];

function openDb() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, DB_VERSION);
    request.onupgradeneeded = () => {
      const db = request.result;
      if (!db.objectStoreNames.contains("accounts")) {
        const accounts = db.createObjectStore("accounts", { keyPath: "id" });
        accounts.createIndex("type", "type", { unique: false });
        accounts.createIndex("name", "name", { unique: false });
      }
      if (!db.objectStoreNames.contains("categories")) {
        const categories = db.createObjectStore("categories", { keyPath: "id" });
        categories.createIndex("name", "name", { unique: true });
        categories.createIndex("sortOrder", "sortOrder", { unique: false });
      }
      if (!db.objectStoreNames.contains("transactions")) {
        const transactions = db.createObjectStore("transactions", { keyPath: "id" });
        transactions.createIndex("date", "date", { unique: false });
        transactions.createIndex("accountId", "accountId", { unique: false });
        transactions.createIndex("categoryId", "categoryId", { unique: false });
      }
      if (!db.objectStoreNames.contains("budgets")) {
        const budgets = db.createObjectStore("budgets", { keyPath: "id" });
        budgets.createIndex("categoryId", "categoryId", { unique: false });
        budgets.createIndex("month", "month", { unique: false });
        budgets.createIndex("categoryMonth", ["categoryId", "month"], { unique: true });
      }
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error ?? new Error("Could not open the local ledger."));
  });
}

function getAll(db, storeName) {
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, "readonly");
    const request = tx.objectStore(storeName).getAll();
    request.onsuccess = () => resolve(request.result ?? []);
    request.onerror = () => reject(request.error ?? new Error("Could not read the local ledger."));
  });
}

export async function loadSnapshot() {
  const db = await openDb();
  const [accounts, categories, transactions, budgets] = await Promise.all([
    getAll(db, "accounts"),
    getAll(db, "categories"),
    getAll(db, "transactions"),
    getAll(db, "budgets"),
  ]);
  return { accounts, categories, transactions, budgets };
}

export async function saveSnapshot(snapshot) {
  const db = await openDb();
  await new Promise((resolve, reject) => {
    const tx = db.transaction(STORES, "readwrite");
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error ?? new Error("Could not save the local ledger."));
    tx.onabort = () => reject(tx.error ?? new Error("Could not save the local ledger."));
    for (const name of STORES) {
      const store = tx.objectStore(name);
      store.clear();
      const rows = snapshot[name] ?? [];
      for (const row of rows) {
        store.add(row);
      }
    }
  });
}

export function downloadText(filename, text, mimeType) {
  const blob = new Blob([text], { type: mimeType || "text/plain;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}
