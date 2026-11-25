import { Button } from "@/components/ui/button";
import { APP_NAME } from "@/constants";

export default function App() {
  return (
    <main className="min-h-svh p-8">
      <h1 className="text-4xl font-bold">{APP_NAME}</h1>
      <p className="mt-2 text-muted-foreground">shadcn/ui is active.</p>
      <Button className="mt-4">Click me</Button>
    </main>
  );
}
  